using System.IO;
using MindedOS.Core;
using MindedOS.Engine;
using MindedOS.Sensor;

namespace MindedOS.Ai;

/// <summary>
/// The AI-Application capability: accumulates the EEG-to-words stream for a fixed
/// window (3 min by default), builds an army-skewed prompt from those words, asks
/// LM Studio to generate a Python program, and saves it as a .py file. The prompt
/// is always written to disk first, so nothing is lost if LM Studio is offline.
/// </summary>
public sealed class AiAppGenerator
{
    private readonly IEegSource _source;
    private readonly RawLexicon _lexicon;
    private readonly AiAppConfig _config;

    private volatile string _currentWord = "—";
    private CancellationTokenSource? _cts;

    public AiAppGenerator(IEegSource source, RawLexicon lexicon, AiAppConfig config)
    {
        _source = source;
        _lexicon = lexicon;
        _config = config;
    }

    public bool IsRunning { get; private set; }

    public event Action<double>? Progress;            // 0..1 over the accumulation window
    public event Action<string>? Status;
    public event Action<string>? Completed;           // saved artifact path
    public event Action<string>? Result;              // the produced text (rewrite kind)
    public event Action<string>? Failed;

    public string OutputDirectory => string.IsNullOrWhiteSpace(_config.OutputDir)
        ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                       "mindedOS", DefaultSubfolder())
        : _config.OutputDir;

    private string DefaultSubfolder() => _config.Kind.ToLowerInvariant() switch
    {
        "article" => "generated_articles",
        "advice" => "generated_advice",
        "algorithm" => "generated_algorithms",
        "slides" => "generated_slides",
        "analysis" => "generated_analysis",
        "artificial" => "generated_artificial",
        "aitheory" => "generated_theories",
        "cloudeval" => "generated_cloud",
        "cognition" => "generated_cognition",
        "gpo" => "generated_security",
        "deeplearning" => "generated_deeplearning",
        "ecommerce" => "generated_ecommerce",
        "emergent" => "generated_behavior",
        "emotional" => "generated_emotions",
        "ethics" => "generated_ethics",
        "face" => "face",
        "gear" => "generated_engineering",
        "healthcare" => "generated_healthcare",
        "humanvsai" => "generated_duel",
        "humanoid" => "generated_humanoid",
        "interaction" => "generated_interactions",
        "mltheory" => "generated_ml",
        "questvr" => "generated_xr",
        "workforce" => "generated_workforce",
        "chip" => "generated_chip",
        "blender" => "generated_prompts",
        "multimodal" => "generated_learning",
        "nlp" => "generated_nlp",
        "pattern" => "generated_patterns",
        "perception" => "generated_perception",
        "planning" => "generated_planning",
        "problemsolving" => "generated_problem_solving",
        "processor" => "generated_processor",
        "quantum" => "generated_quantum",
        "reactive" => "generated_reactive",
        "reasoning" => "generated_reasoning",
        "mas" => "generated_mas",
        "rl" => "generated_rl",
        "robot" => "generated_robot",
        "robotics" => "generated_robotics",
        "selfaware" => "generated_self_awareness",
        "semisup" => "generated_semi_supervised",
        "sensorimotor" => "generated_sensorimotor",
        "smarthouse" => "generated_smart_house",
        "strongai" => "generated_strong_ai",
        "superintelligence" => "generated_superintelligence",
        "supervised" => "generated_supervised",
        "swarm" => "generated_swarm",
        "taskauto" => "generated_taskauto",
        "tom" => "generated_theory_of_mind",
        "transfer" => "generated_transfer",
        "turing" => "generated_turing",
        "unsup" => "generated_unsupervised",
        "vrworld" => "generated_vr",
        "voice" => "generated_voice",
        "weakai" => "generated_weak_ai",
        "rewrite" => "generated_text",
        "rightspeech" => "generated_right_speech",
        "famitracker" => "generated_songs",
        _ => "generated_py",
    };

    private static string PatternContent_RecordedCsv(IReadOnlyList<string> words)
    {
        var sb = new System.Text.StringBuilder("index,word\n");
        for (int i = 0; i < words.Count; i++) sb.AppendLine($"{i},{words[i]}");
        return sb.ToString();
    }

    private static string PatternTopicsCsv(IReadOnlyList<MindedOS.Engine.PatternTopicScore> topics)
    {
        var sb = new System.Text.StringBuilder("topic,percent\n");
        foreach (var t in topics) sb.AppendLine($"{t.Topic},{t.Percent:0.0}");
        return sb.ToString();
    }

    private static string PerceptionRecordedCsv(IReadOnlyList<string> words)
    {
        var sb = new System.Text.StringBuilder("index,word\n");
        for (int i = 0; i < words.Count; i++) sb.AppendLine($"{i},{words[i]}");
        return sb.ToString();
    }

    private static string PerceptionTopicsCsv(IReadOnlyList<MindedOS.Engine.PerceptionScore> topics)
    {
        var sb = new System.Text.StringBuilder("topic,percent\n");
        foreach (var t in topics) sb.AppendLine($"{t.Topic},{t.Percent:0.0}");
        return sb.ToString();
    }

    private static string QuantumConceptsCsv(IReadOnlyList<MindedOS.Engine.QuantumScore> topics)
    {
        var sb = new System.Text.StringBuilder("topic,percent\n");
        foreach (var t in topics) sb.AppendLine($"{t.Topic},{t.Percent:0.0}");
        return sb.ToString();
    }

    public void Cancel() => _cts?.Cancel();

    public async Task RunAsync()
    {
        if (IsRunning) return;
        IsRunning = true;
        _cts = new CancellationTokenSource();
        var ct = _cts.Token;

        // The "artificial brain" kind streams from the computer's processor instead
        // of the shared human source; that local source is disposed when we finish.
        bool useProcessor = string.Equals(_config.Kind, "artificial", StringComparison.OrdinalIgnoreCase);
        var source = useProcessor ? (IEegSource)new ProcessorBrainSource() : _source;

        // EEG accumulators for the window (words + averages that shape the song).
        double attSum = 0, medSum = 0;
        long attCnt = 0, medCnt = 0, bandFrames = 0;
        var bandSums = new long[8];
        var accLock = new object();

        void OnEvent(EegEvent e)
        {
            switch (e)
            {
                case RawEvent r when _lexicon.IsLoaded:
                    _currentWord = _lexicon.WordFor(r.Amplitude);
                    break;
                case AttentionEvent a: lock (accLock) { attSum += a.Level; attCnt++; } break;
                case MeditationEvent m: lock (accLock) { medSum += m.Level; medCnt++; } break;
                case SpectrumEvent sp:
                    lock (accLock)
                    {
                        bandSums[0] += sp.Bands.Delta; bandSums[1] += sp.Bands.Theta;
                        bandSums[2] += sp.Bands.LowAlpha; bandSums[3] += sp.Bands.HighAlpha;
                        bandSums[4] += sp.Bands.LowBeta; bandSums[5] += sp.Bands.HighBeta;
                        bandSums[6] += sp.Bands.LowGamma; bandSums[7] += sp.Bands.MidGamma;
                        bandFrames++;
                    }
                    break;
            }
        }

        string? promptPath = null;
        try
        {
            if (source.State != LinkState.Streaming)
            {
                Status?.Invoke(useProcessor ? "Spinning up the artificial brain…" : "Connecting EEG source…");
                await source.ConnectAsync(ct);
            }

            source.Event += OnEvent;

            // --- accumulate EEG over the window ---
            var accumulator = new WordAccumulator();
            int seconds = Math.Max(1, _config.AccumulateSeconds);
            Status?.Invoke(useProcessor
                ? $"Studying the artificial (processor) brain for {seconds / 60.0:0.#} min…"
                : $"Recording EEG for {seconds / 60.0:0.#} min…");

            var start = DateTime.UtcNow;
            var window = TimeSpan.FromSeconds(seconds);
            while (true)
            {
                ct.ThrowIfCancellationRequested();
                accumulator.Add(_currentWord);
                var elapsed = DateTime.UtcNow - start;
                Progress?.Invoke(Math.Clamp(elapsed / window, 0, 1));
                if (elapsed >= window) break;
                await Task.Delay(250, ct);
            }
            source.Event -= OnEvent; // freeze the accumulators

            bool song = string.Equals(_config.Kind, "famitracker", StringComparison.OrdinalIgnoreCase);
            bool rewrite = string.Equals(_config.Kind, "rewrite", StringComparison.OrdinalIgnoreCase);
            bool article = string.Equals(_config.Kind, "article", StringComparison.OrdinalIgnoreCase);
            bool advice = string.Equals(_config.Kind, "advice", StringComparison.OrdinalIgnoreCase);
            bool algorithm = string.Equals(_config.Kind, "algorithm", StringComparison.OrdinalIgnoreCase);
            bool slides = string.Equals(_config.Kind, "slides", StringComparison.OrdinalIgnoreCase);
            bool analysis = string.Equals(_config.Kind, "analysis", StringComparison.OrdinalIgnoreCase);
            bool blender = string.Equals(_config.Kind, "blender", StringComparison.OrdinalIgnoreCase);
            bool artificial = useProcessor; // kind == "artificial"
            bool aitheory = string.Equals(_config.Kind, "aitheory", StringComparison.OrdinalIgnoreCase);
            bool questvr = string.Equals(_config.Kind, "questvr", StringComparison.OrdinalIgnoreCase);
            bool workforce = string.Equals(_config.Kind, "workforce", StringComparison.OrdinalIgnoreCase);
            bool chip = string.Equals(_config.Kind, "chip", StringComparison.OrdinalIgnoreCase);
            bool cloudeval = string.Equals(_config.Kind, "cloudeval", StringComparison.OrdinalIgnoreCase);
            bool cognition = string.Equals(_config.Kind, "cognition", StringComparison.OrdinalIgnoreCase);
            bool gpo = string.Equals(_config.Kind, "gpo", StringComparison.OrdinalIgnoreCase);
            bool deeplearning = string.Equals(_config.Kind, "deeplearning", StringComparison.OrdinalIgnoreCase);
            bool ecommerce = string.Equals(_config.Kind, "ecommerce", StringComparison.OrdinalIgnoreCase);
            bool emergent = string.Equals(_config.Kind, "emergent", StringComparison.OrdinalIgnoreCase);
            bool emotional = string.Equals(_config.Kind, "emotional", StringComparison.OrdinalIgnoreCase);
            bool ethics = string.Equals(_config.Kind, "ethics", StringComparison.OrdinalIgnoreCase);
            bool face = string.Equals(_config.Kind, "face", StringComparison.OrdinalIgnoreCase);
            bool gear = string.Equals(_config.Kind, "gear", StringComparison.OrdinalIgnoreCase);
            bool healthcare = string.Equals(_config.Kind, "healthcare", StringComparison.OrdinalIgnoreCase);
            bool humanvsai = string.Equals(_config.Kind, "humanvsai", StringComparison.OrdinalIgnoreCase);
            bool humanoid = string.Equals(_config.Kind, "humanoid", StringComparison.OrdinalIgnoreCase);
            bool interaction = string.Equals(_config.Kind, "interaction", StringComparison.OrdinalIgnoreCase);
            bool mltheory = string.Equals(_config.Kind, "mltheory", StringComparison.OrdinalIgnoreCase);
            bool multimodal = string.Equals(_config.Kind, "multimodal", StringComparison.OrdinalIgnoreCase);
            bool nlp = string.Equals(_config.Kind, "nlp", StringComparison.OrdinalIgnoreCase);
            bool pattern = string.Equals(_config.Kind, "pattern", StringComparison.OrdinalIgnoreCase);
            bool perception = string.Equals(_config.Kind, "perception", StringComparison.OrdinalIgnoreCase);
            bool planning = string.Equals(_config.Kind, "planning", StringComparison.OrdinalIgnoreCase);
            bool problemsolving = string.Equals(_config.Kind, "problemsolving", StringComparison.OrdinalIgnoreCase);
            bool processor = string.Equals(_config.Kind, "processor", StringComparison.OrdinalIgnoreCase);
            bool quantum = string.Equals(_config.Kind, "quantum", StringComparison.OrdinalIgnoreCase);
            bool reactive = string.Equals(_config.Kind, "reactive", StringComparison.OrdinalIgnoreCase);
            bool reasoning = string.Equals(_config.Kind, "reasoning", StringComparison.OrdinalIgnoreCase);
            bool mas = string.Equals(_config.Kind, "mas", StringComparison.OrdinalIgnoreCase);
            bool rl = string.Equals(_config.Kind, "rl", StringComparison.OrdinalIgnoreCase);
            bool robot = string.Equals(_config.Kind, "robot", StringComparison.OrdinalIgnoreCase);
            bool robotics = string.Equals(_config.Kind, "robotics", StringComparison.OrdinalIgnoreCase);
            bool selfaware = string.Equals(_config.Kind, "selfaware", StringComparison.OrdinalIgnoreCase);
            bool semisup = string.Equals(_config.Kind, "semisup", StringComparison.OrdinalIgnoreCase);
            bool sensorimotor = string.Equals(_config.Kind, "sensorimotor", StringComparison.OrdinalIgnoreCase);
            bool smarthouse = string.Equals(_config.Kind, "smarthouse", StringComparison.OrdinalIgnoreCase);
            bool strongai = string.Equals(_config.Kind, "strongai", StringComparison.OrdinalIgnoreCase);
            bool superi = string.Equals(_config.Kind, "superintelligence", StringComparison.OrdinalIgnoreCase);
            bool supervised = string.Equals(_config.Kind, "supervised", StringComparison.OrdinalIgnoreCase);
            bool swarm = string.Equals(_config.Kind, "swarm", StringComparison.OrdinalIgnoreCase);
            bool taskauto = string.Equals(_config.Kind, "taskauto", StringComparison.OrdinalIgnoreCase);
            bool tom = string.Equals(_config.Kind, "tom", StringComparison.OrdinalIgnoreCase);
            bool transfer = string.Equals(_config.Kind, "transfer", StringComparison.OrdinalIgnoreCase);
            bool turing = string.Equals(_config.Kind, "turing", StringComparison.OrdinalIgnoreCase);
            bool unsup = string.Equals(_config.Kind, "unsup", StringComparison.OrdinalIgnoreCase);
            bool vrworld = string.Equals(_config.Kind, "vrworld", StringComparison.OrdinalIgnoreCase);
            bool voice = string.Equals(_config.Kind, "voice", StringComparison.OrdinalIgnoreCase);
            bool weakai = string.Equals(_config.Kind, "weakai", StringComparison.OrdinalIgnoreCase);
            bool rightspeech = string.Equals(_config.Kind, "rightspeech", StringComparison.OrdinalIgnoreCase);
            string artifact = song ? "song" : rewrite ? "text" : article ? "article"
                            : advice ? "advice" : algorithm ? "algorithm" : slides ? "slides"
                            : analysis ? "analysis" : blender ? "prompt" : artificial ? "artificial"
                            : aitheory ? "aitheory" : questvr ? "xrprompt" : workforce ? "workforce"
                            : chip ? "chip" : cloudeval ? "cloudeval" : cognition ? "cognition"
                            : gpo ? "gpo" : deeplearning ? "deeplearning" : ecommerce ? "ecommerce"
                            : emergent ? "emergent" : emotional ? "emotional" : ethics ? "ethics"
                            : face ? "face" : gear ? "gear" : healthcare ? "healthcare"
                            : humanvsai ? "duel" : humanoid ? "humanoid"
                            : interaction ? "interaction" : mltheory ? "mltheory"
                            : rightspeech ? "rightspeech" : "app";
            Directory.CreateDirectory(OutputDirectory);
            var stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var baseName = $"eeg_{_config.PromptSkew}_{artifact}_{stamp}";

            // EEG-derived condition: averages, per-band readings, dominant band and profile.
            double avgAtt, avgMed;
            string domKey = "none";
            MentalProfile profile;
            IReadOnlyList<BandReading> bandReadings = Array.Empty<BandReading>();
            lock (accLock)
            {
                avgAtt = attCnt > 0 ? attSum / attCnt : 50;
                avgMed = medCnt > 0 ? medSum / medCnt : 50;
                BandReading? dom = null;
                if (bandFrames > 0)
                {
                    var mean = new BandPowers(
                        (int)(bandSums[0] / bandFrames), (int)(bandSums[1] / bandFrames),
                        (int)(bandSums[2] / bandFrames), (int)(bandSums[3] / bandFrames),
                        (int)(bandSums[4] / bandFrames), (int)(bandSums[5] / bandFrames),
                        (int)(bandSums[6] / bandFrames), (int)(bandSums[7] / bandFrames));
                    bandReadings = BandInterpreter.Interpret(mean);
                    dom = BandInterpreter.DominantBand(bandReadings);
                    domKey = dom?.Key ?? "none";
                }
                profile = MentalProfileClassifier.Classify(avgAtt, avgMed, dom);
            }

            // ===== FamiTracker song: deterministic, EEG-driven, no LM Studio =====
            if (song)
            {
                Status?.Invoke($"Composing (focus {avgAtt:0}, calm {avgMed:0}, dominant {domKey}) from {accumulator.Count} words…");
                var pars = FamiTrackerSong.FromEeg(avgAtt, avgMed, domKey, accumulator.Words, _config.PromptSkew);
                var songText = FamiTrackerSong.Render(pars); // throws if it would be invalid

                var songPath = Path.Combine(OutputDirectory, $"{baseName}.txt");
                File.WriteAllText(songPath, songText);
                File.WriteAllText(Path.Combine(OutputDirectory, $"{baseName}.brief.txt"),
                    $"Title: {pars.Title}\nTempo {pars.Tempo}  Speed {pars.Speed}  " +
                    $"Mode {(pars.Minor ? "minor" : "major")}  Density {pars.Density}\n" +
                    $"focus avg {avgAtt:0}, calm avg {avgMed:0}, dominant {domKey}\n\nwords: {accumulator.Seed()}");

                Status?.Invoke($"Saved {Path.GetFileName(songPath)} — import in FamiTracker (File ▸ Import Text).");
                Completed?.Invoke(songPath);
                return;
            }

            // ===== Right Speech: deterministic, lexicon-locked reordering into speakable text, no LM Studio =====
            if (rightspeech)
            {
                Status?.Invoke($"Arranging {accumulator.Count} brain words into speech…");
                var speech = RightSpeech.Speak(accumulator.Words);

                var speechPath = Path.Combine(OutputDirectory, $"{baseName}.txt");
                File.WriteAllText(speechPath,
                    speech + "\n\n--- gathered words (emission order) ---\n" + accumulator.Seed());

                Status?.Invoke($"Saved {Path.GetFileName(speechPath)} — {accumulator.Count} brain words, reordered.");
                Completed?.Invoke(speechPath);
                return;
            }

            // ===== Augmented Workforce: deterministic 200-agent roster + LM elaboration =====
            if (workforce)
            {
                var wf = WorkforceBuilder.Build(accumulator.Seed());
                string elaboration = WorkforceBuilder.DefaultElaboration(wf);
                try
                {
                    Status?.Invoke("Elaborating the workforce with LM Studio…");
                    using var wclient = new LmStudioClient(_config.LmStudioUrl);
                    string wmodel = _config.Model;
                    if (string.IsNullOrWhiteSpace(wmodel)) wmodel = await wclient.GetFirstModelAsync(ct) ?? "";
                    if (!string.IsNullOrWhiteSpace(wmodel))
                    {
                        var wp = WorkforcePromptBuilder.Build(wf.Agents.Count, accumulator.Seed());
                        var wreply = await wclient.CompleteAsync(wmodel, wp.System, wp.User, ct);
                        var e = RewritePromptBuilder.CleanReply(wreply);
                        if (!string.IsNullOrWhiteSpace(e)) elaboration = e;
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio offline — used the built-in elaboration.");
                }

                var md = WorkforceBuilder.ToMarkdown(wf, elaboration);
                Directory.CreateDirectory(OutputDirectory);
                var wfPath = Path.Combine(OutputDirectory, $"{baseName}.md");
                File.WriteAllText(wfPath, md);

                Result?.Invoke(md);
                Status?.Invoke($"Saved {Path.GetFileName(wfPath)} ({wf.Agents.Count} agents).");
                Completed?.Invoke(wfPath);
                return;
            }

            // ===== Cognition Master: deterministic 1–200% score + LM assessment =====
            if (cognition)
            {
                double score = CognitionIndex.Compute(avgAtt, avgMed, bandReadings);
                string assessment = CognitionIndex.DefaultAssessment(score, avgAtt);
                try
                {
                    Status?.Invoke($"Cognition {score:0}% — validating with LM Studio…");
                    using var cclient = new LmStudioClient(_config.LmStudioUrl);
                    string cmodel = _config.Model;
                    if (string.IsNullOrWhiteSpace(cmodel)) cmodel = await cclient.GetFirstModelAsync(ct) ?? "";
                    if (!string.IsNullOrWhiteSpace(cmodel))
                    {
                        var cp = CognitionPromptBuilder.Build(accumulator.Seed(), seconds, avgAtt, avgMed, domKey, profile, score);
                        var creply = await cclient.CompleteAsync(cmodel, cp.System, cp.User, ct);
                        var a = RewritePromptBuilder.CleanReply(creply);
                        if (!string.IsNullOrWhiteSpace(a)) assessment = a;
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio offline — used the built-in assessment.");
                }

                var cmd = $"# Cognition Master\n\n**COGNITION SCORE: {score:0}% (1–200%) — {CognitionIndex.Tier(score)}**\n\n{assessment}\n";
                Directory.CreateDirectory(OutputDirectory);
                var cpath = Path.Combine(OutputDirectory, $"{baseName}.md");
                File.WriteAllText(cpath, cmd);
                Result?.Invoke(cmd);
                Status?.Invoke($"Cognition {score:0}% — saved {Path.GetFileName(cpath)}.");
                Completed?.Invoke(cpath);
                return;
            }

            // ===== Cybersecurity: deterministic 35-rule GPO baseline + LM elaboration =====
            if (gpo)
            {
                var baseline = GpoBaseline.Build(accumulator.Seed(), avgAtt);
                string elaboration = GpoBaseline.DefaultElaboration(baseline);
                try
                {
                    Status?.Invoke("Composing the security baseline elaboration with LM Studio…");
                    using var gclient = new LmStudioClient(_config.LmStudioUrl);
                    string gmodel = _config.Model;
                    if (string.IsNullOrWhiteSpace(gmodel)) gmodel = await gclient.GetFirstModelAsync(ct) ?? "";
                    if (!string.IsNullOrWhiteSpace(gmodel))
                    {
                        var gp = GpoPromptBuilder.Build(baseline.Codename, baseline.Rules.Count, accumulator.Seed());
                        var greply = await gclient.CompleteAsync(gmodel, gp.System, gp.User, ct);
                        var ge = RewritePromptBuilder.CleanReply(greply);
                        if (!string.IsNullOrWhiteSpace(ge)) elaboration = ge;
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio offline — used the built-in elaboration.");
                }

                var gmd = GpoBaseline.ToMarkdown(baseline, elaboration);
                Directory.CreateDirectory(OutputDirectory);
                var gpath = Path.Combine(OutputDirectory, $"{baseName}.md");
                File.WriteAllText(gpath, gmd);
                Result?.Invoke(gmd);
                Status?.Invoke($"Saved {Path.GetFileName(gpath)} ({baseline.Rules.Count} GPO rules, {(baseline.Strict ? "strict" : "balanced")}).");
                Completed?.Invoke(gpath);
                return;
            }

            // ===== Ethical AI: deterministic 0–100% ethical score + guaranteed 10-slide PPTX =====
            if (ethics)
            {
                double score = EthicsIndex.Compute(avgAtt, avgMed, bandReadings);
                string tier = EthicsIndex.Tier(score);
                IReadOnlyList<SlideContent>? lmSlides = null;
                try
                {
                    Status?.Invoke($"Ethical potential {score:0}% ({tier}) — explaining with LM Studio…");
                    using var eclient = new LmStudioClient(_config.LmStudioUrl);
                    string emodel = _config.Model;
                    if (string.IsNullOrWhiteSpace(emodel)) emodel = await eclient.GetFirstModelAsync(ct) ?? "";
                    if (!string.IsNullOrWhiteSpace(emodel))
                    {
                        var ep = EthicsPromptBuilder.Build(accumulator.Seed(), seconds, score, tier,
                            avgAtt, avgMed, domKey, profile, EthicsIndex.BodyTitles);
                        var ereply = await eclient.CompleteAsync(emodel, ep.System, ep.User, ct);
                        var raw = RewritePromptBuilder.CleanReply(ereply);
                        if (!string.IsNullOrWhiteSpace(raw))
                        {
                            File.WriteAllText(Path.Combine(OutputDirectory, $"{baseName}.md"), raw);
                            lmSlides = PptxArticleWriter.ParseSlides(raw, EthicsIndex.BodyTitles.Count);
                        }
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio offline — used the built-in ethics explanation.");
                }

                // Deterministic title + score slides, then the eight explanatory slides
                // (LM bullets when present, else built-in) — always exactly 10 slides.
                var deck = EthicsIndex.BuildDeck(score, avgAtt, avgMed, domKey, accumulator.Seed(), lmSlides);
                var ethPath = Path.Combine(OutputDirectory, $"{baseName}.pptx");
                PptxArticleWriter.Write(deck, ethPath, string.IsNullOrWhiteSpace(_config.Font) ? "Verdana" : _config.Font);

                Result?.Invoke(EthicsIndex.ToMarkdown(deck));
                Status?.Invoke($"Ethical {score:0}% ({tier}) — saved {Path.GetFileName(ethPath)} ({deck.Count} slides).");
                Completed?.Invoke(ethPath);
                return;
            }

            // ===== Facial Recognition: scan a folder for the face PNG, compare it to the EEG via vision =====
            if (face)
            {
                var faceFile = FaceScan.FindFace(OutputDirectory);
                if (faceFile is null)
                    throw new InvalidOperationException(
                        $"No face image found. Put a .png/.jpg of your face in: {OutputDirectory}");

                Status?.Invoke($"Detecting face in {Path.GetFileName(faceFile)} and comparing to your EEG…");
                string report = FaceScan.OfflineReport(faceFile, accumulator.Seed(), avgAtt, avgMed, domKey, profile);
                try
                {
                    using var fclient = new LmStudioClient(_config.LmStudioUrl);
                    string fmodel = _config.Model;
                    if (string.IsNullOrWhiteSpace(fmodel)) fmodel = await fclient.GetFirstModelAsync(ct) ?? "";
                    if (!string.IsNullOrWhiteSpace(fmodel))
                    {
                        var fp = FaceRecognitionPromptBuilder.Build(accumulator.Seed(), seconds, avgAtt, avgMed, domKey, profile);
                        var imageBytes = await File.ReadAllBytesAsync(faceFile, ct);
                        var freply = await fclient.CompleteWithImageAsync(
                            fmodel, fp.System, fp.User, imageBytes, FaceScan.MimeFor(faceFile), ct);
                        var clean = RewritePromptBuilder.CleanReply(freply);
                        if (!string.IsNullOrWhiteSpace(clean)) report = clean;
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio vision unavailable — saved the EEG-side report only.");
                }

                var facePath = Path.Combine(OutputDirectory, $"{baseName}.md");
                File.WriteAllText(facePath, report);
                Result?.Invoke(report);
                Status?.Invoke($"Saved {Path.GetFileName(facePath)} (face: {Path.GetFileName(faceFile)}).");
                Completed?.Invoke(facePath);
                return;
            }

            // ===== Healthcare AI: EEG deterministically selects drugs from the CSV, LM speculates =====
            if (healthcare)
            {
                string mapFile = string.IsNullOrWhiteSpace(_config.MapFile) ? "eeg_map_drugs.csv" : _config.MapFile;
                string mapPath = MindedOS.Core.DataFile.Resolve(Path.Combine(AppContext.BaseDirectory, "data", mapFile));
                if (!File.Exists(mapPath))
                    throw new InvalidOperationException($"Drug catalog CSV not found: {mapPath}");

                var formulary = DrugFormulary.Load(mapPath);
                var picks = formulary.Select(accumulator.Words, count: 4);
                string combo = string.Join(" + ", picks.Select(p => p.Drug));
                Status?.Invoke($"EEG selected {combo} — speculating with LM Studio…");

                string speculation = DrugFormulary.OfflineSpeculation(picks);
                try
                {
                    using var hclient = new LmStudioClient(_config.LmStudioUrl);
                    string hmodel = _config.Model;
                    if (string.IsNullOrWhiteSpace(hmodel)) hmodel = await hclient.GetFirstModelAsync(ct) ?? "";
                    if (!string.IsNullOrWhiteSpace(hmodel))
                    {
                        var hp = HealthcarePromptBuilder.Build(picks, accumulator.Seed(), seconds, avgAtt, avgMed, domKey, profile);
                        var hreply = await hclient.CompleteAsync(hmodel, hp.System, hp.User, ct);
                        var s = RewritePromptBuilder.CleanReply(hreply);
                        if (!string.IsNullOrWhiteSpace(s)) speculation = s;
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio offline — used the built-in speculation.");
                }

                var md = DrugFormulary.ToMarkdown(picks, speculation, avgAtt, avgMed, domKey, accumulator.Seed());
                var hpath = Path.Combine(OutputDirectory, $"{baseName}.md");
                File.WriteAllText(hpath, md);
                Result?.Invoke(md);
                Status?.Invoke($"Saved {Path.GetFileName(hpath)} — EEG combined {picks.Count} drugs ({combo}).");
                Completed?.Invoke(hpath);
                return;
            }

            // ===== Human vs AI: the generic loop captured the HUMAN EEG; now synthesize the AI EEG and judge =====
            if (humanvsai)
            {
                var human = new Contestant("Human", 0, avgAtt, avgMed, domKey, profile,
                    accumulator.Seed(), ScienceDuel.DistinctCount(accumulator.Words));

                // --- generate the AI (processor) EEG over the same window ---
                Status?.Invoke($"Generating the AI (processor) EEG for {seconds / 60.0:0.#} min…");
                var aiSource = new ProcessorBrainSource();
                await aiSource.ConnectAsync(ct);

                double aAttSum = 0, aMedSum = 0; long aAttCnt = 0, aMedCnt = 0, aBandFrames = 0;
                var aBandSums = new long[8]; var aLock = new object();
                var aiAcc = new WordAccumulator();
                string aiWord = "—";
                void AiEvent(EegEvent e)
                {
                    switch (e)
                    {
                        case RawEvent r when _lexicon.IsLoaded: aiWord = _lexicon.WordFor(r.Amplitude); break;
                        case AttentionEvent a: lock (aLock) { aAttSum += a.Level; aAttCnt++; } break;
                        case MeditationEvent m: lock (aLock) { aMedSum += m.Level; aMedCnt++; } break;
                        case SpectrumEvent sp:
                            lock (aLock)
                            {
                                aBandSums[0] += sp.Bands.Delta; aBandSums[1] += sp.Bands.Theta;
                                aBandSums[2] += sp.Bands.LowAlpha; aBandSums[3] += sp.Bands.HighAlpha;
                                aBandSums[4] += sp.Bands.LowBeta; aBandSums[5] += sp.Bands.HighBeta;
                                aBandSums[6] += sp.Bands.LowGamma; aBandSums[7] += sp.Bands.MidGamma;
                                aBandFrames++;
                            }
                            break;
                    }
                }
                aiSource.Event += AiEvent;
                var aStart = DateTime.UtcNow; var aWindow = TimeSpan.FromSeconds(seconds);
                while (true)
                {
                    ct.ThrowIfCancellationRequested();
                    aiAcc.Add(aiWord);
                    var el = DateTime.UtcNow - aStart;
                    Progress?.Invoke(Math.Clamp(el / aWindow, 0, 1));
                    if (el >= aWindow) break;
                    await Task.Delay(250, ct);
                }
                aiSource.Event -= AiEvent;
                await aiSource.DisconnectAsync();

                double aiAtt, aiMed; string aiDom = "none";
                MentalProfile aiProfile; IReadOnlyList<BandReading> aiBands = Array.Empty<BandReading>();
                lock (aLock)
                {
                    aiAtt = aAttCnt > 0 ? aAttSum / aAttCnt : 50;
                    aiMed = aMedCnt > 0 ? aMedSum / aMedCnt : 50;
                    BandReading? d = null;
                    if (aBandFrames > 0)
                    {
                        var mean = new BandPowers(
                            (int)(aBandSums[0] / aBandFrames), (int)(aBandSums[1] / aBandFrames),
                            (int)(aBandSums[2] / aBandFrames), (int)(aBandSums[3] / aBandFrames),
                            (int)(aBandSums[4] / aBandFrames), (int)(aBandSums[5] / aBandFrames),
                            (int)(aBandSums[6] / aBandFrames), (int)(aBandSums[7] / aBandFrames));
                        aiBands = BandInterpreter.Interpret(mean);
                        d = BandInterpreter.DominantBand(aiBands);
                        aiDom = d?.Key ?? "none";
                    }
                    aiProfile = MentalProfileClassifier.Classify(aiAtt, aiMed, d);
                }
                var aiC = new Contestant("AI", 0, aiAtt, aiMed, aiDom, aiProfile,
                    aiAcc.Seed(), ScienceDuel.DistinctCount(aiAcc.Words));

                // --- deterministic scientific-EEG scores decide the winner ---
                human = human with { Score = ScienceDuel.Score(avgAtt, avgMed, bandReadings, accumulator.Words) };
                aiC = aiC with { Score = ScienceDuel.Score(aiAtt, aiMed, aiBands, aiAcc.Words) };
                var champ = ScienceDuel.Winner(human, aiC);

                string verdict = ScienceDuel.OfflineVerdict(human, aiC);
                try
                {
                    Status?.Invoke($"{champ.Name} leads ({human.Score:0} vs {aiC.Score:0}) — judging with LM Studio…");
                    using var jclient = new LmStudioClient(_config.LmStudioUrl);
                    string jmodel = _config.Model;
                    if (string.IsNullOrWhiteSpace(jmodel)) jmodel = await jclient.GetFirstModelAsync(ct) ?? "";
                    if (!string.IsNullOrWhiteSpace(jmodel))
                    {
                        var jp = HumanVsAiPromptBuilder.Build(human, aiC, seconds);
                        var jreply = await jclient.CompleteAsync(jmodel, jp.System, jp.User, ct);
                        var v = RewritePromptBuilder.CleanReply(jreply);
                        if (!string.IsNullOrWhiteSpace(v)) verdict = v;
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio offline — used the built-in science verdict.");
                }

                var md = ScienceDuel.ToMarkdown(human, aiC, verdict, seconds);
                var dpath = Path.Combine(OutputDirectory, $"{baseName}.md");
                File.WriteAllText(dpath, md);
                Result?.Invoke(md);
                Status?.Invoke($"{champ.Name} wins on science ({human.Score:0} vs {aiC.Score:0}) — saved {Path.GetFileName(dpath)}.");
                Completed?.Invoke(dpath);
                return;
            }

            // ===== Humanoid: deterministic humanoid % + edits + words, LM explains, rendered to PDF =====
            if (humanoid)
            {
                var prof = HumanoidIndex.Compute(avgAtt, avgMed, bandReadings, accumulator.Words);
                Status?.Invoke($"Humanoid {prof.Percent:0}% — {prof.Edits.Count} edit(s) to 100%; explaining with LM Studio…");

                string explanation = HumanoidIndex.OfflineExplanation(prof);
                try
                {
                    using var uclient = new LmStudioClient(_config.LmStudioUrl);
                    string umodel = _config.Model;
                    if (string.IsNullOrWhiteSpace(umodel)) umodel = await uclient.GetFirstModelAsync(ct) ?? "";
                    if (!string.IsNullOrWhiteSpace(umodel))
                    {
                        var up = HumanoidPromptBuilder.Build(prof, accumulator.Seed(), seconds, avgAtt, avgMed, domKey, profile);
                        var ureply = await uclient.CompleteAsync(umodel, up.System, up.User, ct);
                        var e = RewritePromptBuilder.CleanReply(ureply);
                        if (!string.IsNullOrWhiteSpace(e)) explanation = e;
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio offline — used the built-in humanoid explanation.");
                }

                var md = HumanoidIndex.ToMarkdown(prof, explanation, avgAtt, avgMed, domKey, accumulator.Seed());
                File.WriteAllText(Path.Combine(OutputDirectory, $"{baseName}.md"), md);
                var pdfPath = Path.Combine(OutputDirectory, $"{baseName}.pdf");
                PdfArticleWriter.Write(md, pdfPath, string.IsNullOrWhiteSpace(_config.Font) ? "Verdana" : _config.Font);

                Result?.Invoke(md);
                Status?.Invoke($"{prof.Percent:0}% humanoid, {prof.Edits.Count} edit(s) to 100% — saved {Path.GetFileName(pdfPath)}.");
                Completed?.Invoke(pdfPath);
                return;
            }

            // ===== Interaction: the recorded EEG word list becomes a you↔AI chat (.txt) =====
            if (interaction)
            {
                var turns = InteractionLog.Turns(accumulator.Words, 24);

                // save the recorded EEG list as a CSV (the words the brain says to the AI)
                var csvPath = Path.Combine(OutputDirectory, $"{baseName}.csv");
                File.WriteAllText(csvPath, InteractionLog.ToCsv(turns));

                Status?.Invoke($"Recorded {turns.Count} EEG turns — talking to the AI with LM Studio…");
                string transcript = InteractionLog.OfflineTranscript(turns);
                try
                {
                    using var iclient = new LmStudioClient(_config.LmStudioUrl);
                    string imodel = _config.Model;
                    if (string.IsNullOrWhiteSpace(imodel)) imodel = await iclient.GetFirstModelAsync(ct) ?? "";
                    if (!string.IsNullOrWhiteSpace(imodel))
                    {
                        var ip = InteractionPromptBuilder.Build(turns, seconds, avgAtt, avgMed, domKey, profile);
                        var ireply = await iclient.CompleteAsync(imodel, ip.System, ip.User, ct);
                        var t = RewritePromptBuilder.CleanReply(ireply);
                        if (!string.IsNullOrWhiteSpace(t)) transcript = t;
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio offline — used the built-in chat from your EEG list.");
                }

                var txtPath = Path.Combine(OutputDirectory, $"{baseName}.txt");
                File.WriteAllText(txtPath, transcript);
                Result?.Invoke(transcript);
                Status?.Invoke($"Saved chat {Path.GetFileName(txtPath)} and EEG list {Path.GetFileName(csvPath)} ({turns.Count} turns).");
                Completed?.Invoke(txtPath);
                return;
            }

            // ===== Multimodal Learning: deterministic profile + subjects, two LM calls, full package =====
            if (multimodal)
            {
                Directory.CreateDirectory(OutputDirectory);

                // 1) translated_eeg.txt — recording stats header + decoded word stream
                int distinctWords = accumulator.Words.Distinct(StringComparer.OrdinalIgnoreCase).Count();
                string translated =
                    "# Translated EEG\n" +
                    $"recorded_seconds: {seconds}\n" +
                    $"words: {accumulator.Count}\n" +
                    $"distinct_words: {distinctWords}\n" +
                    $"avg_attention: {avgAtt:0}\n" +
                    $"avg_meditation: {avgMed:0}\n" +
                    $"dominant_band: {domKey}\n\n" +
                    (string.IsNullOrWhiteSpace(accumulator.Seed()) ? "(no words captured)" : accumulator.Seed());
                File.WriteAllText(Path.Combine(OutputDirectory, "translated_eeg.txt"), translated);

                // 2) deterministic learning profile + subject ranking
                var lp = LearningProfile.Compute(avgAtt, avgMed, bandReadings, accumulator.Words);
                var dataDir = Path.Combine(AppContext.BaseDirectory, "data");
                var subjects = LearningSubjects.DetectFromFile(dataDir, accumulator.Words);
                lp.WriteProfileCsv(Path.Combine(OutputDirectory, "learning_profile.csv"), subjects, domKey);
                lp.AppendHistory(Path.Combine(OutputDirectory, "learning_history.csv"), subjects, domKey);

                // 3) LM call 1 — analysis Markdown (fallback to the deterministic analysis)
                string mmAnalysis = LearningContent.DefaultAnalysis(lp, subjects, avgAtt, avgMed, domKey);
                try
                {
                    Status?.Invoke("Analyzing your learning profile with LM Studio…");
                    using var mclient = new LmStudioClient(_config.LmStudioUrl);
                    string mmodel = _config.Model;
                    if (string.IsNullOrWhiteSpace(mmodel)) mmodel = await mclient.GetFirstModelAsync(ct) ?? "";
                    if (!string.IsNullOrWhiteSpace(mmodel))
                    {
                        var mp = MultimodalPromptBuilder.Build(accumulator.Seed(), seconds, avgAtt, avgMed, domKey, profile, lp, subjects);
                        var mreply = await mclient.CompleteAsync(mmodel, mp.System, mp.User, ct);
                        var a = RewritePromptBuilder.CleanReply(mreply);
                        if (!string.IsNullOrWhiteSpace(a)) mmAnalysis = a;
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio offline — used the built-in learning analysis.");
                }

                // 4) report (.md + .docx), curriculum.md, knowledge_graph.md
                string font = string.IsNullOrWhiteSpace(_config.Font) ? "Verdana" : _config.Font;
                string report = LearningContent.ReportMarkdown(lp, subjects, avgAtt, avgMed, domKey, seconds) + "\n\n" + mmAnalysis;
                File.WriteAllText(Path.Combine(OutputDirectory, "multimodal_learning_report.md"), report);
                DocxArticleWriter.Write(report, Path.Combine(OutputDirectory, "multimodal_learning_report.docx"), font);
                File.WriteAllText(Path.Combine(OutputDirectory, "curriculum.md"),
                    LearningContent.ExtractSection(mmAnalysis, "Curriculum") ?? LearningContent.DefaultCurriculum(lp, subjects));
                File.WriteAllText(Path.Combine(OutputDirectory, "knowledge_graph.md"),
                    LearningContent.ExtractSection(mmAnalysis, "Knowledge Graph") ?? LearningContent.DefaultKnowledgeGraph(subjects));

                // 5) LM call 2 — 10-slide deck (fallback to the deterministic deck)
                var deck = LearningContent.DefaultDeck(lp, subjects, avgAtt, avgMed, domKey);
                try
                {
                    Status?.Invoke("Building your 10-slide learning deck with LM Studio…");
                    using var sclient = new LmStudioClient(_config.LmStudioUrl);
                    string smodel = _config.Model;
                    if (string.IsNullOrWhiteSpace(smodel)) smodel = await sclient.GetFirstModelAsync(ct) ?? "";
                    if (!string.IsNullOrWhiteSpace(smodel))
                    {
                        var sp = MultimodalSlidesPromptBuilder.Build(accumulator.Seed(), seconds, avgAtt, avgMed, domKey, profile, lp, subjects);
                        var sreply = await sclient.CompleteAsync(smodel, sp.System, sp.User, ct);
                        var parsed = PptxArticleWriter.ParseSlides(RewritePromptBuilder.CleanReply(sreply), 10);
                        if (parsed.Count == 10) deck = parsed; // else keep the deterministic 10-slide deck
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio offline — used the built-in 10-slide deck.");
                }
                PptxArticleWriter.Write(deck, Path.Combine(OutputDirectory, "multimodal_learning_presentation.pptx"), font);

                Result?.Invoke(report);
                Status?.Invoke($"Saved your multimodal learning package to {OutputDirectory}.");
                Completed?.Invoke(Path.Combine(OutputDirectory, "multimodal_learning_report.docx"));
                return;
            }

            // ===== Natural Language Processing: deterministic NLP core + 5 LM calls, full package =====
            if (nlp)
            {
                Directory.CreateDirectory(OutputDirectory);
                var nlpWords = accumulator.Words;
                string nlpSeed = accumulator.Seed();
                int nlpDistinct = nlpWords.Distinct(StringComparer.OrdinalIgnoreCase).Count();

                // deterministic core (always written first)
                File.WriteAllText(Path.Combine(OutputDirectory, "recorded_eeg.csv"), NlpContent.RecordedEegCsv(nlpWords));
                string nlpStats =
                    "# Translated EEG\n" +
                    $"recorded_seconds: {seconds}\n" +
                    $"words: {accumulator.Count}\n" +
                    $"distinct_words: {nlpDistinct}\n" +
                    $"avg_attention: {avgAtt:0}\n" +
                    $"avg_meditation: {avgMed:0}\n" +
                    $"dominant_band: {domKey}\n\n";
                File.WriteAllText(Path.Combine(OutputDirectory, "translated_eeg.txt"), NlpContent.TranslatedText(nlpStats, nlpWords));

                var dataDir = Path.Combine(AppContext.BaseDirectory, "data");
                var nlpTopics = NlpTopics.DetectFromFile(dataDir, nlpWords);
                var nlpProf = NlpProfile.Compute(avgAtt, avgMed, bandReadings, nlpWords, nlpTopics);

                File.WriteAllText(Path.Combine(OutputDirectory, "tokens.csv"), NlpContent.TokensCsv(nlpWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "brain_vocabulary.csv"), NlpContent.VocabularyCsv(nlpWords, nlpTopics));
                File.WriteAllText(Path.Combine(OutputDirectory, "topics.csv"), NlpProfile.TopicsCsv(nlpTopics));
                File.WriteAllText(Path.Combine(OutputDirectory, "thought_analysis.csv"), NlpProfile.ThoughtCsv(avgAtt, bandReadings, nlpWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "communication_profile.csv"), NlpProfile.CommunicationCsv(avgAtt, avgMed, bandReadings, nlpWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "brain_knowledge_graph.md"), NlpContent.KnowledgeGraph(nlpTopics, nlpWords));
                NlpProfile.AppendHistory(Path.Combine(OutputDirectory, "nlp_history.csv"), nlpProf, nlpTopics);

                string nlpFont = string.IsNullOrWhiteSpace(_config.Font) ? "Verdana" : _config.Font;

                // LM 1 — POS + NER CSVs (parse two '# POS' / '# ENTITIES' sections; fallback per-CSV)
                string posCsv = NlpContent.DefaultPosCsv(nlpWords);
                string nerCsv = NlpContent.DefaultEntitiesCsv(nlpWords, nlpTopics);
                try
                {
                    Status?.Invoke("Tagging parts of speech and entities with LM Studio…");
                    using var c1 = new LmStudioClient(_config.LmStudioUrl);
                    string m1 = _config.Model;
                    if (string.IsNullOrWhiteSpace(m1)) m1 = await c1.GetFirstModelAsync(ct) ?? "";
                    if (!string.IsNullOrWhiteSpace(m1))
                    {
                        var p1 = NlpPromptBuilder.BuildLinguistics(nlpSeed);
                        var r1 = RewritePromptBuilder.CleanReply(await c1.CompleteAsync(m1, p1.System, p1.User, ct));
                        var pos = NlpContent.ExtractCsvSection(r1, "# POS", "word,pos");
                        var ner = NlpContent.ExtractCsvSection(r1, "# ENTITIES", "entity,type");
                        if (!string.IsNullOrWhiteSpace(pos)) posCsv = pos;
                        if (!string.IsNullOrWhiteSpace(ner)) nerCsv = ner;
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio offline — used built-in POS/NER.");
                }
                File.WriteAllText(Path.Combine(OutputDirectory, "parts_of_speech.csv"), posCsv);
                File.WriteAllText(Path.Combine(OutputDirectory, "entities.csv"), nerCsv);

                // LM 2 — semantic report
                string nlpSemantic = NlpContent.DefaultSemanticReport(nlpProf, nlpTopics, nlpWords);
                try
                {
                    Status?.Invoke("Writing the semantic report with LM Studio…");
                    using var c2 = new LmStudioClient(_config.LmStudioUrl);
                    string m2 = _config.Model;
                    if (string.IsNullOrWhiteSpace(m2)) m2 = await c2.GetFirstModelAsync(ct) ?? "";
                    if (!string.IsNullOrWhiteSpace(m2))
                    {
                        var p2 = NlpPromptBuilder.BuildSemantic(nlpSeed, avgAtt, avgMed, domKey, profile, nlpTopics);
                        var r2 = RewritePromptBuilder.CleanReply(await c2.CompleteAsync(m2, p2.System, p2.User, ct));
                        if (!string.IsNullOrWhiteSpace(r2)) nlpSemantic = r2;
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio offline — used built-in semantic report.");
                }
                File.WriteAllText(Path.Combine(OutputDirectory, "semantic_report.txt"), nlpSemantic);

                // LM 3 — research paper -> .docx
                string research = NlpContent.DefaultResearchMarkdown(nlpProf, nlpTopics, nlpWords, avgAtt, avgMed, domKey);
                try
                {
                    Status?.Invoke("Writing the research paper with LM Studio…");
                    using var c3 = new LmStudioClient(_config.LmStudioUrl);
                    string m3 = _config.Model;
                    if (string.IsNullOrWhiteSpace(m3)) m3 = await c3.GetFirstModelAsync(ct) ?? "";
                    if (!string.IsNullOrWhiteSpace(m3))
                    {
                        var p3 = NlpPromptBuilder.BuildResearch(nlpSeed, seconds, avgAtt, avgMed, domKey, profile, nlpTopics);
                        var r3 = RewritePromptBuilder.CleanReply(await c3.CompleteAsync(m3, p3.System, p3.User, ct));
                        if (!string.IsNullOrWhiteSpace(r3)) research = r3;
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio offline — used built-in research paper.");
                }
                DocxArticleWriter.Write(research, Path.Combine(OutputDirectory, "nlp_brain_research.docx"), nlpFont);

                // LM 4 — 10-slide deck (accept only a full 10-slide LM deck)
                var nlpDeck = NlpContent.DefaultDeck(nlpProf, nlpTopics, avgAtt, avgMed, domKey);
                try
                {
                    Status?.Invoke("Building the 10-slide deck with LM Studio…");
                    using var c4 = new LmStudioClient(_config.LmStudioUrl);
                    string m4 = _config.Model;
                    if (string.IsNullOrWhiteSpace(m4)) m4 = await c4.GetFirstModelAsync(ct) ?? "";
                    if (!string.IsNullOrWhiteSpace(m4))
                    {
                        var p4 = NlpPromptBuilder.BuildSlides(nlpSeed, avgAtt, avgMed, domKey, nlpTopics);
                        var r4 = RewritePromptBuilder.CleanReply(await c4.CompleteAsync(m4, p4.System, p4.User, ct));
                        var parsed = PptxArticleWriter.ParseSlides(r4, 10);
                        if (parsed.Count == 10) nlpDeck = parsed; // else keep the deterministic 10-slide deck
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio offline — used built-in 10-slide deck.");
                }
                PptxArticleWriter.Write(nlpDeck, Path.Combine(OutputDirectory, "nlp_brain_analysis.pptx"), nlpFont);

                // LM 5 — questions + chat (split '# QUESTIONS' / '# CHAT'; fallback per-section)
                string questions = NlpContent.DefaultQuestions(nlpWords, nlpTopics);
                string chat = NlpContent.DefaultChatLog(nlpWords, nlpTopics);
                try
                {
                    Status?.Invoke("Generating questions and chat with LM Studio…");
                    using var c5 = new LmStudioClient(_config.LmStudioUrl);
                    string m5 = _config.Model;
                    if (string.IsNullOrWhiteSpace(m5)) m5 = await c5.GetFirstModelAsync(ct) ?? "";
                    if (!string.IsNullOrWhiteSpace(m5))
                    {
                        var p5 = NlpPromptBuilder.BuildQuestionsAndChat(nlpSeed, nlpTopics);
                        var r5 = RewritePromptBuilder.CleanReply(await c5.CompleteAsync(m5, p5.System, p5.User, ct));
                        var q = NlpContent.ExtractTextSection(r5, "# QUESTIONS", "# CHAT");
                        var ch = NlpContent.ExtractTextSection(r5, "# CHAT", null);
                        if (!string.IsNullOrWhiteSpace(q)) questions = q;
                        if (!string.IsNullOrWhiteSpace(ch)) chat = ch;
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio offline — used built-in questions and chat.");
                }
                File.WriteAllText(Path.Combine(OutputDirectory, "generated_questions.txt"), questions);
                File.WriteAllText(Path.Combine(OutputDirectory, "brain_chat_log.txt"), chat);

                Result?.Invoke(NlpContent.Dashboard(nlpProf, nlpTopics) + "\n\n" + nlpSemantic);
                Status?.Invoke($"Saved your NLP package to {OutputDirectory}.");
                Completed?.Invoke(Path.Combine(OutputDirectory, "nlp_brain_research.docx"));
                return;
            }

            // ===== Pattern Recognition: deterministic per-session + cross-session scan + 4 LM calls =====
            if (pattern)
            {
                Directory.CreateDirectory(OutputDirectory);
                var patWords = accumulator.Words;
                string patSeed = accumulator.Seed();
                int patDistinct = patWords.Distinct(StringComparer.OrdinalIgnoreCase).Count();
                var dataDir = Path.Combine(AppContext.BaseDirectory, "data");

                // recorded_eeg.csv (latest) + translated_eeg.txt
                File.WriteAllText(Path.Combine(OutputDirectory, "recorded_eeg.csv"), PatternContent_RecordedCsv(patWords));
                string patStats =
                    "# Translated EEG\n" +
                    $"recorded_seconds: {seconds}\n" +
                    $"words: {accumulator.Count}\n" +
                    $"distinct_words: {patDistinct}\n" +
                    $"avg_attention: {avgAtt:0}\n" +
                    $"avg_meditation: {avgMed:0}\n" +
                    $"dominant_band: {domKey}\n\n";
                File.WriteAllText(Path.Combine(OutputDirectory, "translated_eeg.txt"), NlpContent.TranslatedText(patStats, patWords));

                // deterministic per-session signature + topics + states
                var patTopics = PatternTopics.DetectFromFile(dataDir, patWords);
                var patSig = CognitiveSignature.Compute(avgAtt, avgMed, bandReadings, patWords);
                var patStates = CognitiveSignature.BrainStates(patSig, avgAtt, avgMed);
                string patTop = patTopics.Count > 0 ? patTopics[0].Topic : "General";

                File.WriteAllText(Path.Combine(OutputDirectory, "word_patterns.csv"), PatternContent.WordPatternsCsv(patWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "thought_patterns.csv"), PatternContent.ThoughtPatternsCsv(patWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "topic_patterns.csv"), PatternTopicsCsv(patTopics));
                File.WriteAllText(Path.Combine(OutputDirectory, "cognitive_signature.csv"), CognitiveSignature.SignatureCsv(patSig));
                File.WriteAllText(Path.Combine(OutputDirectory, "brain_states.csv"), CognitiveSignature.BrainStatesCsv(patSig, avgAtt, avgMed));
                File.WriteAllText(Path.Combine(OutputDirectory, "knowledge_patterns.csv"), PatternContent.KnowledgePatternsCsv(patWords, patTopics));
                File.WriteAllText(Path.Combine(OutputDirectory, "synthetic_pattern_comparison.csv"), CognitiveSignature.SyntheticComparisonCsv(patSig));

                // cross-session scan (prior recorded_eeg_*.csv + csv_files/) — current first
                var current = new PatternScan.ScannedSession("current", DateTime.Now, patSig, patTop, patWords.Count);
                var priors = PatternScan.Scan(OutputDirectory, dataDir);
                var sessions = new List<PatternScan.ScannedSession> { current };
                sessions.AddRange(priors);

                File.WriteAllText(Path.Combine(OutputDirectory, "session_comparison.csv"), PatternScan.SessionComparisonCsv(sessions, current));
                File.WriteAllText(Path.Combine(OutputDirectory, "brain_clusters.csv"), PatternScan.BrainClustersCsv(sessions));
                File.WriteAllText(Path.Combine(OutputDirectory, "similarity_matrix.csv"), PatternScan.SimilarityMatrixCsv(sessions));
                File.WriteAllText(Path.Combine(OutputDirectory, "network_rankings.csv"), PatternScan.NetworkRankingsCsv(sessions));
                File.WriteAllText(Path.Combine(OutputDirectory, "trend_analysis.csv"), PatternScan.TrendAnalysisCsv(sessions));
                PatternScan.AppendHistory(Path.Combine(OutputDirectory, "pattern_history.csv"), patSig, patTop);

                // save this run as a timestamped recording so future runs can scan it
                var patStamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                File.WriteAllText(Path.Combine(OutputDirectory, $"recorded_eeg_{patStamp}.csv"), PatternContent_RecordedCsv(patWords));

                string patFont = string.IsNullOrWhiteSpace(_config.Font) ? "Verdana" : _config.Font;

                // LM 1 — hidden patterns
                string patHidden = PatternContent.DefaultHiddenPatterns(patWords, patTopics, patSig);
                try
                {
                    Status?.Invoke("Mining hidden patterns with LM Studio…");
                    using var c1 = new LmStudioClient(_config.LmStudioUrl);
                    string m1 = _config.Model;
                    if (string.IsNullOrWhiteSpace(m1)) m1 = await c1.GetFirstModelAsync(ct) ?? "";
                    if (!string.IsNullOrWhiteSpace(m1))
                    {
                        var p1 = PatternPromptBuilder.BuildHidden(patSeed, patSig, patTopics);
                        var r1 = RewritePromptBuilder.CleanReply(await c1.CompleteAsync(m1, p1.System, p1.User, ct));
                        if (!string.IsNullOrWhiteSpace(r1)) patHidden = r1;
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio offline — used built-in hidden patterns.");
                }
                File.WriteAllText(Path.Combine(OutputDirectory, "hidden_patterns.txt"), patHidden);

                // LM 2 — future patterns
                string patFuture = PatternContent.DefaultFuturePatterns(patTopics, patSig);
                try
                {
                    Status?.Invoke("Forecasting future patterns with LM Studio…");
                    using var c2 = new LmStudioClient(_config.LmStudioUrl);
                    string m2 = _config.Model;
                    if (string.IsNullOrWhiteSpace(m2)) m2 = await c2.GetFirstModelAsync(ct) ?? "";
                    if (!string.IsNullOrWhiteSpace(m2))
                    {
                        var p2 = PatternPromptBuilder.BuildFuture(patSig, patTopics);
                        var r2 = RewritePromptBuilder.CleanReply(await c2.CompleteAsync(m2, p2.System, p2.User, ct));
                        if (!string.IsNullOrWhiteSpace(r2)) patFuture = r2;
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio offline — used built-in future patterns.");
                }
                File.WriteAllText(Path.Combine(OutputDirectory, "future_patterns.txt"), patFuture);

                // LM 3 — research paper -> .docx
                string patResearch = PatternContent.DefaultResearchMarkdown(patSig, patTopics, patWords, avgAtt, avgMed, domKey);
                try
                {
                    Status?.Invoke("Writing the pattern report with LM Studio…");
                    using var c3 = new LmStudioClient(_config.LmStudioUrl);
                    string m3 = _config.Model;
                    if (string.IsNullOrWhiteSpace(m3)) m3 = await c3.GetFirstModelAsync(ct) ?? "";
                    if (!string.IsNullOrWhiteSpace(m3))
                    {
                        var p3 = PatternPromptBuilder.BuildResearch(patSeed, seconds, patSig, patTopics, avgAtt, avgMed, domKey, profile);
                        var r3 = RewritePromptBuilder.CleanReply(await c3.CompleteAsync(m3, p3.System, p3.User, ct));
                        if (!string.IsNullOrWhiteSpace(r3)) patResearch = r3;
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio offline — used built-in pattern report.");
                }
                DocxArticleWriter.Write(patResearch, Path.Combine(OutputDirectory, "pattern_recognition_report.docx"), patFont);

                // LM 4 — 10-slide deck (accept only a full 10-slide LM deck)
                var patDeck = PatternContent.DefaultDeck(patSig, patTopics, patWords, avgAtt, avgMed, domKey);
                try
                {
                    Status?.Invoke("Building the 10-slide deck with LM Studio…");
                    using var c4 = new LmStudioClient(_config.LmStudioUrl);
                    string m4 = _config.Model;
                    if (string.IsNullOrWhiteSpace(m4)) m4 = await c4.GetFirstModelAsync(ct) ?? "";
                    if (!string.IsNullOrWhiteSpace(m4))
                    {
                        var p4 = PatternPromptBuilder.BuildSlides(patSig, patTopics, avgAtt, avgMed, domKey);
                        var r4 = RewritePromptBuilder.CleanReply(await c4.CompleteAsync(m4, p4.System, p4.User, ct));
                        var parsed = PptxArticleWriter.ParseSlides(r4, 10);
                        if (parsed.Count == 10) patDeck = parsed; // else keep the deterministic 10-slide deck
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio offline — used built-in 10-slide deck.");
                }
                PptxArticleWriter.Write(patDeck, Path.Combine(OutputDirectory, "pattern_recognition_analysis.pptx"), patFont);

                Result?.Invoke(PatternContent.Dashboard(patSig, patTopics, patStates) + "\n\n" + patHidden);
                Status?.Invoke($"Saved your pattern-recognition package to {OutputDirectory}.");
                Completed?.Invoke(Path.Combine(OutputDirectory, "pattern_recognition_report.docx"));
                return;
            }

            // ===== Perception: deterministic scores + scan + optional image vision + 3 LM calls =====
            if (perception)
            {
                Directory.CreateDirectory(OutputDirectory);
                var perWords = accumulator.Words;
                string perSeed = accumulator.Seed();
                int perDistinct = perWords.Distinct(StringComparer.OrdinalIgnoreCase).Count();
                var dataDir = Path.Combine(AppContext.BaseDirectory, "data");

                // recorded_eeg.csv + translated_eeg.txt
                File.WriteAllText(Path.Combine(OutputDirectory, "recorded_eeg.csv"), PerceptionRecordedCsv(perWords));
                string perStats =
                    "# Translated EEG\n" +
                    $"recorded_seconds: {seconds}\n" +
                    $"words: {accumulator.Count}\n" +
                    $"distinct_words: {perDistinct}\n" +
                    $"avg_attention: {avgAtt:0}\n" +
                    $"avg_meditation: {avgMed:0}\n" +
                    $"dominant_band: {domKey}\n\n";
                File.WriteAllText(Path.Combine(OutputDirectory, "translated_eeg.txt"), NlpContent.TranslatedText(perStats, perWords));

                // deterministic scores + interest rankings
                var perCategories = PerceptionTopics.DetectFromFile(dataDir, perWords, "perception_topics.csv");
                var perObjects = PerceptionTopics.DetectFromFile(dataDir, perWords, "perception_objects.csv");
                var perDash = PerceptionProfile.Dashboard(avgAtt, avgMed, bandReadings, perWords);
                string perTop = perCategories.Count > 0 ? perCategories[0].Topic : "General";

                File.WriteAllText(Path.Combine(OutputDirectory, "environmental_awareness.csv"), PerceptionProfile.EnvironmentalAwarenessCsv(avgAtt, avgMed, bandReadings, perWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "attention_analysis.csv"), PerceptionProfile.AttentionAnalysisCsv(avgAtt, bandReadings, perWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "perception_profile.csv"), PerceptionProfile.PerceptionStylesCsv(avgAtt, avgMed, bandReadings, perWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "curiosity_metrics.csv"), PerceptionProfile.CuriosityMetricsCsv(avgAtt, bandReadings, perWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "object_interest_profile.csv"), PerceptionTopicsCsv(perObjects));
                File.WriteAllText(Path.Combine(OutputDirectory, "perception_categories.csv"), PerceptionTopicsCsv(perCategories));
                File.WriteAllText(Path.Combine(OutputDirectory, "perception_patterns.csv"), PerceptionContent.PerceptionPatternsCsv(perWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "artificial_perception_comparison.csv"), PerceptionContent.ArtificialComparisonCsv(perDash));

                // multi-session trends + history
                var perSessions = PerceptionScan.Scan(OutputDirectory, dataDir);
                File.WriteAllText(Path.Combine(OutputDirectory, "perception_trends.csv"),
                    PerceptionScan.TrendsCsv(perSessions.Count > 0 ? perSessions
                        : new[] { new PerceptionScan.PerceptionSession("current", DateTime.Now, perDash[0].Value, perDash[2].Value, perDash[3].Value, perTop) }));
                PerceptionProfile.AppendHistory(Path.Combine(OutputDirectory, "perception_history.csv"), avgAtt, avgMed, bandReadings, perWords, perTop);

                // save this run as a timestamped recording so future runs can scan it
                var perStamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                File.WriteAllText(Path.Combine(OutputDirectory, $"recorded_eeg_{perStamp}.csv"), PerceptionRecordedCsv(perWords));

                string perFont = string.IsNullOrWhiteSpace(_config.Font) ? "Verdana" : _config.Font;

                // image perception (optional images/ folder, LM vision, capped at 3)
                string perImageCsv = PerceptionContent.ImagePlaceholderCsv();
                try
                {
                    var imgDir = Path.Combine(OutputDirectory, "images");
                    if (Directory.Exists(imgDir))
                    {
                        var imgs = Directory.EnumerateFiles(imgDir)
                            .Where(f => new[] { ".png", ".jpg", ".jpeg", ".gif", ".webp" }.Contains(Path.GetExtension(f).ToLowerInvariant()))
                            .Take(3).ToList();
                        if (imgs.Count > 0)
                        {
                            Status?.Invoke($"Comparing {imgs.Count} image(s) to your EEG with LM Studio vision…");
                            using var ic = new LmStudioClient(_config.LmStudioUrl);
                            string im = _config.Model;
                            if (string.IsNullOrWhiteSpace(im)) im = await ic.GetFirstModelAsync(ct) ?? "";
                            var sb = new System.Text.StringBuilder("image,concept_match,visual_interest_alignment,perception_consistency\n");
                            foreach (var img in imgs)
                            {
                                string row = $"{Path.GetFileName(img)},50,50,50";
                                if (!string.IsNullOrWhiteSpace(im))
                                {
                                    var ip = PerceptionPromptBuilder.BuildImage(perSeed);
                                    var bytes = await File.ReadAllBytesAsync(img, ct);
                                    var visionReply = RewritePromptBuilder.CleanReply(
                                        await ic.CompleteWithImageAsync(im, ip.System, ip.User, bytes, PerceptionContent.MimeFor(img), ct));
                                    var nums = System.Text.RegularExpressions.Regex.Matches(visionReply, "\\d{1,3}");
                                    if (nums.Count >= 3) row = $"{Path.GetFileName(img)},{nums[0].Value},{nums[1].Value},{nums[2].Value}";
                                }
                                sb.AppendLine(row);
                            }
                            perImageCsv = sb.ToString();
                        }
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio vision unavailable — used the image placeholder.");
                }
                File.WriteAllText(Path.Combine(OutputDirectory, "image_perception_comparison.csv"), perImageCsv);

                // LM 1 — narratives (5 marked sections)
                string perImag = PerceptionContent.DefaultImagination(perCategories, perWords);
                string perModels = PerceptionContent.DefaultMentalModels(perCategories, perWords);
                string perSit = PerceptionContent.DefaultSituational(perCategories);
                string perFuture = PerceptionContent.DefaultFutureVision(perCategories);
                string perKnow = PerceptionContent.DefaultKnowledgeDiscovery(perCategories, perWords);
                try
                {
                    Status?.Invoke("Analyzing perception narratives with LM Studio…");
                    using var c1 = new LmStudioClient(_config.LmStudioUrl);
                    string m1 = _config.Model;
                    if (string.IsNullOrWhiteSpace(m1)) m1 = await c1.GetFirstModelAsync(ct) ?? "";
                    if (!string.IsNullOrWhiteSpace(m1))
                    {
                        var p1 = PerceptionPromptBuilder.BuildNarratives(perSeed, perCategories, avgAtt, avgMed, domKey, profile);
                        var r1 = RewritePromptBuilder.CleanReply(await c1.CompleteAsync(m1, p1.System, p1.User, ct));
                        var imag = NlpContent.ExtractTextSection(r1, "# IMAGINATION", "# MENTAL MODELS");
                        var models = NlpContent.ExtractTextSection(r1, "# MENTAL MODELS", "# SITUATIONAL");
                        var sit = NlpContent.ExtractTextSection(r1, "# SITUATIONAL", "# FUTURE VISION");
                        var fut = NlpContent.ExtractTextSection(r1, "# FUTURE VISION", "# KNOWLEDGE");
                        var know = NlpContent.ExtractTextSection(r1, "# KNOWLEDGE", null);
                        if (!string.IsNullOrWhiteSpace(imag)) perImag = imag;
                        if (!string.IsNullOrWhiteSpace(models)) perModels = models;
                        if (!string.IsNullOrWhiteSpace(sit)) perSit = sit;
                        if (!string.IsNullOrWhiteSpace(fut)) perFuture = fut;
                        if (!string.IsNullOrWhiteSpace(know)) perKnow = know;
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio offline — used built-in perception narratives.");
                }
                File.WriteAllText(Path.Combine(OutputDirectory, "visual_imagination_report.txt"), perImag);
                File.WriteAllText(Path.Combine(OutputDirectory, "mental_models.md"), perModels);
                File.WriteAllText(Path.Combine(OutputDirectory, "situational_interpretation.txt"), perSit);
                File.WriteAllText(Path.Combine(OutputDirectory, "future_vision_report.txt"), perFuture);
                File.WriteAllText(Path.Combine(OutputDirectory, "knowledge_discovery_report.txt"), perKnow);

                // LM 2 — research paper -> .docx
                string perResearch = PerceptionContent.DefaultResearchMarkdown(perCategories, perWords, avgAtt, avgMed, domKey);
                try
                {
                    Status?.Invoke("Writing the perception report with LM Studio…");
                    using var c2 = new LmStudioClient(_config.LmStudioUrl);
                    string m2 = _config.Model;
                    if (string.IsNullOrWhiteSpace(m2)) m2 = await c2.GetFirstModelAsync(ct) ?? "";
                    if (!string.IsNullOrWhiteSpace(m2))
                    {
                        var p2 = PerceptionPromptBuilder.BuildResearch(perSeed, seconds, perCategories, avgAtt, avgMed, domKey, profile);
                        var r2 = RewritePromptBuilder.CleanReply(await c2.CompleteAsync(m2, p2.System, p2.User, ct));
                        if (!string.IsNullOrWhiteSpace(r2)) perResearch = r2;
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio offline — used built-in perception report.");
                }
                DocxArticleWriter.Write(perResearch, Path.Combine(OutputDirectory, "perception_analysis_report.docx"), perFont);

                // LM 3 — 10-slide deck (accept only a full 10-slide LM deck)
                var perDeck = PerceptionContent.DefaultDeck(perCategories, perWords, avgAtt, avgMed, domKey);
                try
                {
                    Status?.Invoke("Building the 10-slide deck with LM Studio…");
                    using var c3 = new LmStudioClient(_config.LmStudioUrl);
                    string m3 = _config.Model;
                    if (string.IsNullOrWhiteSpace(m3)) m3 = await c3.GetFirstModelAsync(ct) ?? "";
                    if (!string.IsNullOrWhiteSpace(m3))
                    {
                        var p3 = PerceptionPromptBuilder.BuildSlides(perCategories, avgAtt, avgMed, domKey);
                        var r3 = RewritePromptBuilder.CleanReply(await c3.CompleteAsync(m3, p3.System, p3.User, ct));
                        var parsed = PptxArticleWriter.ParseSlides(r3, 10);
                        if (parsed.Count == 10) perDeck = parsed; // else keep the deterministic 10-slide deck
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio offline — used built-in 10-slide deck.");
                }
                PptxArticleWriter.Write(perDeck, Path.Combine(OutputDirectory, "perception_analysis.pptx"), perFont);

                Result?.Invoke(PerceptionContent.Dashboard(perDash, perCategories) + "\n\n" + perImag);
                Status?.Invoke($"Saved your perception package to {OutputDirectory}.");
                Completed?.Invoke(Path.Combine(OutputDirectory, "perception_analysis_report.docx"));
                return;
            }

            // ===== Planning: deterministic structures + scan + 3 LM calls =====
            if (planning)
            {
                Directory.CreateDirectory(OutputDirectory);
                var planWords = accumulator.Words;
                string planSeed = accumulator.Seed();
                int planDistinct = planWords.Distinct(StringComparer.OrdinalIgnoreCase).Count();
                var dataDir = Path.Combine(AppContext.BaseDirectory, "data");

                // recorded_eeg.csv + translated_eeg.txt
                File.WriteAllText(Path.Combine(OutputDirectory, "recorded_eeg.csv"), NlpContent.RecordedEegCsv(planWords));
                string planStats =
                    "# Translated EEG\n" +
                    $"recorded_seconds: {seconds}\n" +
                    $"words: {accumulator.Count}\n" +
                    $"distinct_words: {planDistinct}\n" +
                    $"avg_attention: {avgAtt:0}\n" +
                    $"avg_meditation: {avgMed:0}\n" +
                    $"dominant_band: {domKey}\n\n";
                File.WriteAllText(Path.Combine(OutputDirectory, "translated_eeg.txt"), NlpContent.TranslatedText(planStats, planWords));

                // deterministic structures + scores
                var planTopics = PlanningTopics.DetectFromFile(dataDir, planWords);
                var planDash = PlanningProfile.Dashboard(avgAtt, avgMed, bandReadings, planWords);
                string planTop = planTopics.Count > 0 ? planTopics[0].Domain : "General";

                File.WriteAllText(Path.Combine(OutputDirectory, "identified_goals.csv"), PlanningContent.IdentifiedGoalsCsv(planWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "priority_analysis.csv"), PlanningContent.PriorityAnalysisCsv(planTopics));
                File.WriteAllText(Path.Combine(OutputDirectory, "intention_analysis.csv"), PlanningProfile.IntentionCsv(avgAtt, avgMed, bandReadings, planWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "timeline_plans.csv"), PlanningContent.TimelinePlansCsv(planTopics));
                File.WriteAllText(Path.Combine(OutputDirectory, "resource_requirements.csv"), PlanningContent.ResourceRequirementsCsv(planTopics));
                File.WriteAllText(Path.Combine(OutputDirectory, "opportunity_analysis.csv"), PlanningContent.OpportunityAnalysisCsv(planTopics));
                File.WriteAllText(Path.Combine(OutputDirectory, "goal_forecasts.csv"), PlanningProfile.GoalForecastsCsv(avgAtt, avgMed, bandReadings, planWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "task_breakdown.csv"), PlanningContent.TaskBreakdownCsv(planWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "planning_scores.csv"), PlanningProfile.PlanningScoresCsv(avgAtt, avgMed, bandReadings, planWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "project_rankings.csv"), PlanningContent.ProjectRankingsCsv(planTopics));

                // multi-user network (prior recordings + csv_files/) — include the current run
                var planScores = PlanningProfile.PlanningScores(avgAtt, avgMed, bandReadings, planWords);
                var planForecasts = PlanningProfile.GoalForecasts(avgAtt, avgMed, bandReadings, planWords);
                var planRows = new List<PlanningScan.PlanningProfileRow>
                {
                    new("current", planScores[0].Value, planScores[2].Value, planScores[4].Value, planForecasts[0].Value),
                };
                planRows.AddRange(PlanningScan.Scan(OutputDirectory, dataDir));
                File.WriteAllText(Path.Combine(OutputDirectory, "planning_network_analysis.csv"), PlanningScan.NetworkCsv(planRows));
                PlanningProfile.AppendHistory(Path.Combine(OutputDirectory, "planning_history.csv"), avgAtt, avgMed, bandReadings, planWords, planTop);

                // save this run as a timestamped recording so future runs can scan it
                var planStamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                File.WriteAllText(Path.Combine(OutputDirectory, $"recorded_eeg_{planStamp}.csv"), NlpContent.RecordedEegCsv(planWords));

                string planFont = string.IsNullOrWhiteSpace(_config.Font) ? "Verdana" : _config.Font;

                // LM 1 — narratives (6 marked sections)
                string planStrategic = PlanningContent.DefaultStrategicPlans(planTopics, planWords);
                string planRoadmap = PlanningContent.DefaultRoadmap(planTopics);
                string planDecision = PlanningContent.DefaultDecisionSupport(planTopics);
                string planScenarios = PlanningContent.DefaultScenarios(planTopics);
                string planResearchPlans = PlanningContent.DefaultResearchPlans(planTopics);
                string planAdvisor = PlanningContent.DefaultAdvisor(planTopics, planDash);
                try
                {
                    Status?.Invoke("Drafting strategic plans with LM Studio…");
                    using var c1 = new LmStudioClient(_config.LmStudioUrl);
                    string m1 = _config.Model;
                    if (string.IsNullOrWhiteSpace(m1)) m1 = await c1.GetFirstModelAsync(ct) ?? "";
                    if (!string.IsNullOrWhiteSpace(m1))
                    {
                        var p1 = PlanningPromptBuilder.BuildNarratives(planSeed, planTopics, avgAtt, avgMed, domKey, profile);
                        var r1 = RewritePromptBuilder.CleanReply(await c1.CompleteAsync(m1, p1.System, p1.User, ct));
                        var sp = NlpContent.ExtractTextSection(r1, "# STRATEGIC PLANS", "# ROADMAP");
                        var rm = NlpContent.ExtractTextSection(r1, "# ROADMAP", "# DECISION SUPPORT");
                        var ds = NlpContent.ExtractTextSection(r1, "# DECISION SUPPORT", "# SCENARIOS");
                        var sc = NlpContent.ExtractTextSection(r1, "# SCENARIOS", "# RESEARCH PLANS");
                        var rp = NlpContent.ExtractTextSection(r1, "# RESEARCH PLANS", "# ADVISOR");
                        var ad = NlpContent.ExtractTextSection(r1, "# ADVISOR", null);
                        if (!string.IsNullOrWhiteSpace(sp)) planStrategic = sp;
                        if (!string.IsNullOrWhiteSpace(rm)) planRoadmap = rm;
                        if (!string.IsNullOrWhiteSpace(ds)) planDecision = ds;
                        if (!string.IsNullOrWhiteSpace(sc)) planScenarios = sc;
                        if (!string.IsNullOrWhiteSpace(rp)) planResearchPlans = rp;
                        if (!string.IsNullOrWhiteSpace(ad)) planAdvisor = ad;
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio offline — used built-in planning narratives.");
                }
                File.WriteAllText(Path.Combine(OutputDirectory, "strategic_plans.md"), planStrategic);
                File.WriteAllText(Path.Combine(OutputDirectory, "project_roadmap.md"), planRoadmap);
                File.WriteAllText(Path.Combine(OutputDirectory, "decision_support_report.txt"), planDecision);
                File.WriteAllText(Path.Combine(OutputDirectory, "future_scenarios.md"), planScenarios);
                File.WriteAllText(Path.Combine(OutputDirectory, "research_plans.md"), planResearchPlans);
                File.WriteAllText(Path.Combine(OutputDirectory, "planning_advisor_report.txt"), planAdvisor);

                // LM 2 — research paper -> .docx
                string planReport = PlanningContent.DefaultResearchMarkdown(planTopics, planWords, avgAtt, avgMed, domKey);
                try
                {
                    Status?.Invoke("Writing the planning report with LM Studio…");
                    using var c2 = new LmStudioClient(_config.LmStudioUrl);
                    string m2 = _config.Model;
                    if (string.IsNullOrWhiteSpace(m2)) m2 = await c2.GetFirstModelAsync(ct) ?? "";
                    if (!string.IsNullOrWhiteSpace(m2))
                    {
                        var p2 = PlanningPromptBuilder.BuildResearch(planSeed, seconds, planTopics, avgAtt, avgMed, domKey, profile);
                        var r2 = RewritePromptBuilder.CleanReply(await c2.CompleteAsync(m2, p2.System, p2.User, ct));
                        if (!string.IsNullOrWhiteSpace(r2)) planReport = r2;
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio offline — used built-in planning report.");
                }
                DocxArticleWriter.Write(planReport, Path.Combine(OutputDirectory, "planning_analysis_report.docx"), planFont);

                // LM 3 — 10-slide deck (accept only a full 10-slide LM deck)
                var planDeck = PlanningContent.DefaultDeck(planTopics, planWords, avgAtt, avgMed, domKey);
                try
                {
                    Status?.Invoke("Building the 10-slide deck with LM Studio…");
                    using var c3 = new LmStudioClient(_config.LmStudioUrl);
                    string m3 = _config.Model;
                    if (string.IsNullOrWhiteSpace(m3)) m3 = await c3.GetFirstModelAsync(ct) ?? "";
                    if (!string.IsNullOrWhiteSpace(m3))
                    {
                        var p3 = PlanningPromptBuilder.BuildSlides(planTopics, avgAtt, avgMed, domKey);
                        var r3 = RewritePromptBuilder.CleanReply(await c3.CompleteAsync(m3, p3.System, p3.User, ct));
                        var parsed = PptxArticleWriter.ParseSlides(r3, 10);
                        if (parsed.Count == 10) planDeck = parsed; // else keep the deterministic 10-slide deck
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio offline — used built-in 10-slide deck.");
                }
                PptxArticleWriter.Write(planDeck, Path.Combine(OutputDirectory, "planning_analysis.pptx"), planFont);

                Result?.Invoke(PlanningContent.Dashboard(planDash, planTopics) + "\n\n" + planStrategic);
                Status?.Invoke($"Saved your planning package to {OutputDirectory}.");
                Completed?.Invoke(Path.Combine(OutputDirectory, "planning_analysis_report.docx"));
                return;
            }

            // ===== Problem Solving: deterministic scores + scan + 3 LM calls =====
            if (problemsolving)
            {
                Directory.CreateDirectory(OutputDirectory);
                var psWords = accumulator.Words;
                string psSeed = accumulator.Seed();
                int psDistinct = psWords.Distinct(StringComparer.OrdinalIgnoreCase).Count();
                var dataDir = Path.Combine(AppContext.BaseDirectory, "data");

                // recorded_eeg.csv + translated_eeg.txt
                File.WriteAllText(Path.Combine(OutputDirectory, "recorded_eeg.csv"), NlpContent.RecordedEegCsv(psWords));
                string psStats =
                    "# Translated EEG\n" +
                    $"recorded_seconds: {seconds}\n" +
                    $"words: {accumulator.Count}\n" +
                    $"distinct_words: {psDistinct}\n" +
                    $"avg_attention: {avgAtt:0}\n" +
                    $"avg_meditation: {avgMed:0}\n" +
                    $"dominant_band: {domKey}\n\n";
                File.WriteAllText(Path.Combine(OutputDirectory, "translated_eeg.txt"), NlpContent.TranslatedText(psStats, psWords));

                // deterministic scores + challenge ranking
                var psChallenges = ChallengeTopics.DetectFromFile(dataDir, psWords);
                var psDash = ProblemSolvingProfile.Dashboard(avgAtt, avgMed, bandReadings, psWords);
                var psArchetypes = ProblemSolvingProfile.SolverArchetypes(avgAtt, avgMed, bandReadings, psWords);
                string psTop = psChallenges.Count > 0 ? psChallenges[0].Challenge : "General";

                File.WriteAllText(Path.Combine(OutputDirectory, "logical_reasoning.csv"), ProblemSolvingProfile.LogicalReasoningCsv(avgAtt, avgMed, bandReadings, psWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "problem_decomposition.csv"), ProblemSolvingProfile.ProblemDecompositionCsv(avgAtt, bandReadings, psWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "strategy_analysis.csv"), ProblemSolvingProfile.StrategyAnalysisCsv(avgAtt, avgMed, bandReadings, psWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "innovation_profile.csv"), ProblemSolvingProfile.InnovationProfileCsv(avgAtt, bandReadings, psWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "decision_analysis.csv"), ProblemSolvingProfile.DecisionAnalysisCsv(avgAtt, avgMed, bandReadings, psWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "challenge_profile.csv"), ProblemSolvingContent.ChallengeProfileCsv(psChallenges));
                File.WriteAllText(Path.Combine(OutputDirectory, "problem_solver_profile.csv"), ProblemSolvingContent.SolverProfileCsv(psArchetypes));
                File.WriteAllText(Path.Combine(OutputDirectory, "knowledge_extraction.csv"), ProblemSolvingContent.KnowledgeExtractionCsv(psWords, psChallenges));

                // multi-session trends + history (include the current run, then prior recordings)
                var psSessions = new List<ProblemSolvingScan.SolvingSession>
                {
                    new("current", DateTime.Now, psDash[1].Value, psDash[2].Value, psDash[3].Value),
                };
                psSessions.AddRange(ProblemSolvingScan.Scan(OutputDirectory));
                File.WriteAllText(Path.Combine(OutputDirectory, "problem_solving_trends.csv"),
                    ProblemSolvingScan.TrendsCsv(psSessions));
                ProblemSolvingProfile.AppendHistory(Path.Combine(OutputDirectory, "problem_solving_history.csv"), avgAtt, avgMed, bandReadings, psWords, psTop);

                // save this run as a timestamped recording so future runs can scan it
                var psStamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                File.WriteAllText(Path.Combine(OutputDirectory, $"recorded_eeg_{psStamp}.csv"), NlpContent.RecordedEegCsv(psWords));

                string psFont = string.IsNullOrWhiteSpace(_config.Font) ? "Verdana" : _config.Font;

                // LM 1 — narratives (5 marked sections)
                string psSolutions = ProblemSolvingContent.DefaultSolutionGeneration(psChallenges, psWords);
                string psSims = ProblemSolvingContent.DefaultSimulations(psChallenges);
                string psRoot = ProblemSolvingContent.DefaultRootCause(psChallenges, psWords);
                string psMulti = ProblemSolvingContent.DefaultMultiSolution(psChallenges);
                string psFuture = ProblemSolvingContent.DefaultFuturePredictions(psChallenges);
                try
                {
                    Status?.Invoke("Generating solutions with LM Studio…");
                    using var c1 = new LmStudioClient(_config.LmStudioUrl);
                    string m1 = _config.Model;
                    if (string.IsNullOrWhiteSpace(m1)) m1 = await c1.GetFirstModelAsync(ct) ?? "";
                    if (!string.IsNullOrWhiteSpace(m1))
                    {
                        var p1 = ProblemSolvingPromptBuilder.BuildNarratives(psSeed, psChallenges, avgAtt, avgMed, domKey, profile);
                        var r1 = RewritePromptBuilder.CleanReply(await c1.CompleteAsync(m1, p1.System, p1.User, ct));
                        var so = NlpContent.ExtractTextSection(r1, "# SOLUTIONS", "# SIMULATIONS");
                        var si = NlpContent.ExtractTextSection(r1, "# SIMULATIONS", "# ROOT CAUSE");
                        var ro = NlpContent.ExtractTextSection(r1, "# ROOT CAUSE", "# MULTI SOLUTION");
                        var mu = NlpContent.ExtractTextSection(r1, "# MULTI SOLUTION", "# FUTURE");
                        var fu = NlpContent.ExtractTextSection(r1, "# FUTURE", null);
                        if (!string.IsNullOrWhiteSpace(so)) psSolutions = so;
                        if (!string.IsNullOrWhiteSpace(si)) psSims = si;
                        if (!string.IsNullOrWhiteSpace(ro)) psRoot = ro;
                        if (!string.IsNullOrWhiteSpace(mu)) psMulti = mu;
                        if (!string.IsNullOrWhiteSpace(fu)) psFuture = fu;
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio offline — used built-in problem-solving narratives.");
                }
                File.WriteAllText(Path.Combine(OutputDirectory, "solution_generation.txt"), psSolutions);
                File.WriteAllText(Path.Combine(OutputDirectory, "problem_simulations.txt"), psSims);
                File.WriteAllText(Path.Combine(OutputDirectory, "root_cause_analysis.txt"), psRoot);
                File.WriteAllText(Path.Combine(OutputDirectory, "multi_solution_report.txt"), psMulti);
                File.WriteAllText(Path.Combine(OutputDirectory, "future_challenge_predictions.txt"), psFuture);

                // LM 2 — research paper -> .docx
                string psReport = ProblemSolvingContent.DefaultResearchMarkdown(psChallenges, psWords, avgAtt, avgMed, domKey);
                try
                {
                    Status?.Invoke("Writing the problem-solving report with LM Studio…");
                    using var c2 = new LmStudioClient(_config.LmStudioUrl);
                    string m2 = _config.Model;
                    if (string.IsNullOrWhiteSpace(m2)) m2 = await c2.GetFirstModelAsync(ct) ?? "";
                    if (!string.IsNullOrWhiteSpace(m2))
                    {
                        var p2 = ProblemSolvingPromptBuilder.BuildResearch(psSeed, seconds, psChallenges, avgAtt, avgMed, domKey, profile);
                        var r2 = RewritePromptBuilder.CleanReply(await c2.CompleteAsync(m2, p2.System, p2.User, ct));
                        if (!string.IsNullOrWhiteSpace(r2)) psReport = r2;
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio offline — used built-in problem-solving report.");
                }
                DocxArticleWriter.Write(psReport, Path.Combine(OutputDirectory, "problem_solving_report.docx"), psFont);

                // LM 3 — 10-slide deck (accept only a full 10-slide LM deck)
                var psDeck = ProblemSolvingContent.DefaultDeck(psChallenges, psWords, avgAtt, avgMed, domKey);
                try
                {
                    Status?.Invoke("Building the 10-slide deck with LM Studio…");
                    using var c3 = new LmStudioClient(_config.LmStudioUrl);
                    string m3 = _config.Model;
                    if (string.IsNullOrWhiteSpace(m3)) m3 = await c3.GetFirstModelAsync(ct) ?? "";
                    if (!string.IsNullOrWhiteSpace(m3))
                    {
                        var p3 = ProblemSolvingPromptBuilder.BuildSlides(psChallenges, avgAtt, avgMed, domKey);
                        var r3 = RewritePromptBuilder.CleanReply(await c3.CompleteAsync(m3, p3.System, p3.User, ct));
                        var parsed = PptxArticleWriter.ParseSlides(r3, 10);
                        if (parsed.Count == 10) psDeck = parsed; // else keep the deterministic 10-slide deck
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio offline — used built-in 10-slide deck.");
                }
                PptxArticleWriter.Write(psDeck, Path.Combine(OutputDirectory, "problem_solving_analysis.pptx"), psFont);

                Result?.Invoke(ProblemSolvingContent.Dashboard(psDash, psChallenges) + "\n\n" + psSolutions);
                Status?.Invoke($"Saved your problem-solving package to {OutputDirectory}.");
                Completed?.Invoke(Path.Combine(OutputDirectory, "problem_solving_report.docx"));
                return;
            }

            // ===== Processor: deterministic score-sets + cores + CPU-EEG + scan + 3 LM calls =====
            if (processor)
            {
                Directory.CreateDirectory(OutputDirectory);
                var prWords = accumulator.Words;
                string prSeed = accumulator.Seed();
                int prDistinct = prWords.Distinct(StringComparer.OrdinalIgnoreCase).Count();
                var dataDir = Path.Combine(AppContext.BaseDirectory, "data");

                // recorded_eeg.csv + translated_eeg.txt
                File.WriteAllText(Path.Combine(OutputDirectory, "recorded_eeg.csv"), NlpContent.RecordedEegCsv(prWords));
                string prStats =
                    "# Translated EEG\n" +
                    $"recorded_seconds: {seconds}\n" +
                    $"words: {accumulator.Count}\n" +
                    $"distinct_words: {prDistinct}\n" +
                    $"avg_attention: {avgAtt:0}\n" +
                    $"avg_meditation: {avgMed:0}\n" +
                    $"dominant_band: {domKey}\n\n";
                File.WriteAllText(Path.Combine(OutputDirectory, "translated_eeg.txt"), NlpContent.TranslatedText(prStats, prWords));

                // deterministic score-sets
                var prCores = CoreTopics.DetectFromFile(dataDir, prWords);
                var prDash = ProcessorProfile.Dashboard(avgAtt, avgMed, bandReadings, prWords);
                string prTop = prCores.Count > 0 ? prCores[0].Core : "Logic";

                File.WriteAllText(Path.Combine(OutputDirectory, "input_processing.csv"), ProcessorProfile.InputProcessingCsv(avgAtt, bandReadings, prWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "processing_pipeline.csv"), ProcessorProfile.ProcessingPipelineCsv(avgAtt, avgMed, bandReadings, prWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "throughput_analysis.csv"), ProcessorProfile.ThroughputAnalysisCsv(avgAtt, bandReadings, prWords, seconds));
                File.WriteAllText(Path.Combine(OutputDirectory, "processing_speed.csv"), ProcessorProfile.ProcessingSpeedCsv(avgAtt, bandReadings, prWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "logic_processing.csv"), ProcessorProfile.LogicProcessingCsv(avgAtt, bandReadings, prWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "parallel_processing.csv"), ProcessorProfile.ParallelProcessingCsv(bandReadings, prWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "memory_processing.csv"), ProcessorProfile.MemoryProcessingCsv(avgAtt, avgMed, bandReadings, prWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "scheduler_analysis.csv"), ProcessorProfile.SchedulerAnalysisCsv(avgAtt, bandReadings, prWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "decision_processing.csv"), ProcessorProfile.DecisionProcessingCsv(avgAtt, avgMed, bandReadings, prWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "multicore_brain.csv"), ProcessorContent.MulticoreBrainCsv(prCores, prWords));

                // CPU-EEG + processor comparison (human vs CPU vs AI)
                File.WriteAllText(Path.Combine(OutputDirectory, "cpu_processor_eeg.csv"), ProcessorContent.CpuProcessorEegCsv());
                var prCpuDash = ProcessorProfile.Dashboard(50, 50, Array.Empty<BandReading>(), ProcessorContent.CpuWords());
                File.WriteAllText(Path.Combine(OutputDirectory, "processor_comparison.csv"), ProcessorContent.ProcessorComparisonCsv(prDash, prCpuDash));

                // multi-session trends (current + priors) + history
                var prSessions = new List<ProcessorScan.ProcessorSession>
                {
                    new("current", DateTime.Now, prDash[0].Value, prDash[2].Value, prDash[4].Value),
                };
                prSessions.AddRange(ProcessorScan.Scan(OutputDirectory));
                File.WriteAllText(Path.Combine(OutputDirectory, "processor_trends.csv"), ProcessorScan.TrendsCsv(prSessions));
                ProcessorProfile.AppendHistory(Path.Combine(OutputDirectory, "processor_history.csv"), avgAtt, avgMed, bandReadings, prWords, prTop);

                // save this run as a timestamped recording so future runs can scan it
                var prStamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                File.WriteAllText(Path.Combine(OutputDirectory, $"recorded_eeg_{prStamp}.csv"), NlpContent.RecordedEegCsv(prWords));

                string prFont = string.IsNullOrWhiteSpace(_config.Font) ? "Verdana" : _config.Font;

                // LM 1 — narratives (3 marked sections)
                string prTask = ProcessorContent.DefaultTaskProcessing(prCores, prWords);
                string prBottleneck = ProcessorContent.DefaultBottleneck(prCores, prWords);
                string prOptimization = ProcessorContent.DefaultOptimization(prCores);
                try
                {
                    Status?.Invoke("Modeling task processing with LM Studio…");
                    using var c1 = new LmStudioClient(_config.LmStudioUrl);
                    string m1 = _config.Model;
                    if (string.IsNullOrWhiteSpace(m1)) m1 = await c1.GetFirstModelAsync(ct) ?? "";
                    if (!string.IsNullOrWhiteSpace(m1))
                    {
                        var p1 = ProcessorPromptBuilder.BuildNarratives(prSeed, prCores, avgAtt, avgMed, domKey, profile);
                        var r1 = RewritePromptBuilder.CleanReply(await c1.CompleteAsync(m1, p1.System, p1.User, ct));
                        var ta = NlpContent.ExtractTextSection(r1, "# TASK PROCESSING", "# BOTTLENECKS");
                        var bo = NlpContent.ExtractTextSection(r1, "# BOTTLENECKS", "# OPTIMIZATION");
                        var op = NlpContent.ExtractTextSection(r1, "# OPTIMIZATION", null);
                        if (!string.IsNullOrWhiteSpace(ta)) prTask = ta;
                        if (!string.IsNullOrWhiteSpace(bo)) prBottleneck = bo;
                        if (!string.IsNullOrWhiteSpace(op)) prOptimization = op;
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio offline — used built-in processor narratives.");
                }
                File.WriteAllText(Path.Combine(OutputDirectory, "task_processing_report.txt"), prTask);
                File.WriteAllText(Path.Combine(OutputDirectory, "bottleneck_report.txt"), prBottleneck);
                File.WriteAllText(Path.Combine(OutputDirectory, "processor_optimization.txt"), prOptimization);

                // LM 2 — research paper -> .docx
                string prReport = ProcessorContent.DefaultResearchMarkdown(prCores, prWords, avgAtt, avgMed, domKey);
                try
                {
                    Status?.Invoke("Writing the processor report with LM Studio…");
                    using var c2 = new LmStudioClient(_config.LmStudioUrl);
                    string m2 = _config.Model;
                    if (string.IsNullOrWhiteSpace(m2)) m2 = await c2.GetFirstModelAsync(ct) ?? "";
                    if (!string.IsNullOrWhiteSpace(m2))
                    {
                        var p2 = ProcessorPromptBuilder.BuildResearch(prSeed, seconds, prCores, avgAtt, avgMed, domKey, profile);
                        var r2 = RewritePromptBuilder.CleanReply(await c2.CompleteAsync(m2, p2.System, p2.User, ct));
                        if (!string.IsNullOrWhiteSpace(r2)) prReport = r2;
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio offline — used built-in processor report.");
                }
                DocxArticleWriter.Write(prReport, Path.Combine(OutputDirectory, "processor_analysis_report.docx"), prFont);

                // LM 3 — 10-slide deck (accept only a full 10-slide LM deck)
                var prDeck = ProcessorContent.DefaultDeck(prCores, prWords, avgAtt, avgMed, domKey);
                try
                {
                    Status?.Invoke("Building the 10-slide deck with LM Studio…");
                    using var c3 = new LmStudioClient(_config.LmStudioUrl);
                    string m3 = _config.Model;
                    if (string.IsNullOrWhiteSpace(m3)) m3 = await c3.GetFirstModelAsync(ct) ?? "";
                    if (!string.IsNullOrWhiteSpace(m3))
                    {
                        var p3 = ProcessorPromptBuilder.BuildSlides(prCores, avgAtt, avgMed, domKey);
                        var r3 = RewritePromptBuilder.CleanReply(await c3.CompleteAsync(m3, p3.System, p3.User, ct));
                        var parsed = PptxArticleWriter.ParseSlides(r3, 10);
                        if (parsed.Count == 10) prDeck = parsed; // else keep the deterministic 10-slide deck
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio offline — used built-in 10-slide deck.");
                }
                PptxArticleWriter.Write(prDeck, Path.Combine(OutputDirectory, "processor_analysis.pptx"), prFont);

                Result?.Invoke(ProcessorContent.Dashboard(prDash, prCores) + "\n\n" + prTask);
                Status?.Invoke($"Saved your processor package to {OutputDirectory}.");
                Completed?.Invoke(Path.Combine(OutputDirectory, "processor_analysis_report.docx"));
                return;
            }

            // ===== Quantum Computing: deterministic concepts/scores + 3 LM calls =====
            if (quantum)
            {
                Directory.CreateDirectory(OutputDirectory);
                var qWords = accumulator.Words;
                string qSeed = accumulator.Seed();
                int qDistinct = qWords.Distinct(StringComparer.OrdinalIgnoreCase).Count();
                var dataDir = Path.Combine(AppContext.BaseDirectory, "data");

                // recorded_eeg.csv + translated_eeg.txt
                File.WriteAllText(Path.Combine(OutputDirectory, "recorded_eeg.csv"), NlpContent.RecordedEegCsv(qWords));
                string qStats =
                    "# Translated EEG\n" +
                    $"recorded_seconds: {seconds}\n" +
                    $"words: {accumulator.Count}\n" +
                    $"distinct_words: {qDistinct}\n" +
                    $"avg_attention: {avgAtt:0}\n" +
                    $"avg_meditation: {avgMed:0}\n" +
                    $"dominant_band: {domKey}\n\n";
                File.WriteAllText(Path.Combine(OutputDirectory, "translated_eeg.txt"), NlpContent.TranslatedText(qStats, qWords));

                // deterministic concepts + interests + scores
                var qConcepts = QuantumTopics.DetectFromFile(dataDir, qWords, "quantum_concepts.csv");
                var qInterests = QuantumTopics.DetectFromFile(dataDir, qWords, "quantum_interests.csv");
                var qScores = QuantumProfile.Scores(avgAtt, avgMed, bandReadings, qWords);
                string qTop = qConcepts.Count > 0 ? qConcepts[0].Topic : "Quantum Computing";

                File.WriteAllText(Path.Combine(OutputDirectory, "quantum_concepts.csv"), QuantumConceptsCsv(qConcepts));
                File.WriteAllText(Path.Combine(OutputDirectory, "quantum_learning_profile.csv"), QuantumConceptsCsv(qInterests));
                File.WriteAllText(Path.Combine(OutputDirectory, "quantum_vocabulary.csv"), QuantumContent.VocabularyCsv());
                File.WriteAllText(Path.Combine(OutputDirectory, "quantum_simulations.csv"), QuantumContent.SimulationsCsv());
                File.WriteAllText(Path.Combine(OutputDirectory, "quantum_scores.csv"), QuantumProfile.ScoresCsv(avgAtt, avgMed, bandReadings, qWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "quantum_researcher_comparison.csv"), QuantumContent.ResearcherComparisonCsv(qWords));

                // multi-session history
                QuantumProfile.AppendHistory(Path.Combine(OutputDirectory, "quantum_history.csv"), avgAtt, avgMed, bandReadings, qWords, qTop);

                // save this run as a timestamped recording so future runs can scan it
                var qStamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                File.WriteAllText(Path.Combine(OutputDirectory, $"recorded_eeg_{qStamp}.csv"), NlpContent.RecordedEegCsv(qWords));

                string qFont = string.IsNullOrWhiteSpace(_config.Font) ? "Verdana" : _config.Font;

                // LM 1 — narratives (8 marked sections)
                string qAlgorithms = QuantumContent.DefaultAlgorithms(qConcepts);
                string qResearchTopics = QuantumContent.DefaultResearchTopics(qConcepts);
                string qProblemSolving = QuantumContent.DefaultProblemSolving(qConcepts);
                string qTheories = QuantumContent.DefaultTheories(qConcepts, qWords);
                string qArchitectures = QuantumContent.DefaultArchitectures(qConcepts);
                string qAiReport = QuantumContent.DefaultAiReport(qConcepts);
                string qCurriculum = QuantumContent.DefaultCurriculum(qConcepts);
                string qKnowledge = QuantumContent.DefaultKnowledgeGraph(qConcepts);
                try
                {
                    Status?.Invoke("Generating quantum educational material with LM Studio…");
                    using var c1 = new LmStudioClient(_config.LmStudioUrl);
                    string m1 = _config.Model;
                    if (string.IsNullOrWhiteSpace(m1)) m1 = await c1.GetFirstModelAsync(ct) ?? "";
                    if (!string.IsNullOrWhiteSpace(m1))
                    {
                        var p1 = QuantumPromptBuilder.BuildNarratives(qSeed, qConcepts, avgAtt, avgMed, domKey, profile);
                        var r1 = RewritePromptBuilder.CleanReply(await c1.CompleteAsync(m1, p1.System, p1.User, ct));
                        var al = NlpContent.ExtractTextSection(r1, "# ALGORITHMS", "# RESEARCH TOPICS");
                        var rt = NlpContent.ExtractTextSection(r1, "# RESEARCH TOPICS", "# PROBLEM SOLVING");
                        var ps = NlpContent.ExtractTextSection(r1, "# PROBLEM SOLVING", "# THEORIES");
                        var th = NlpContent.ExtractTextSection(r1, "# THEORIES", "# ARCHITECTURES");
                        var ar = NlpContent.ExtractTextSection(r1, "# ARCHITECTURES", "# AI REPORT");
                        var air = NlpContent.ExtractTextSection(r1, "# AI REPORT", "# CURRICULUM");
                        var cu = NlpContent.ExtractTextSection(r1, "# CURRICULUM", "# KNOWLEDGE GRAPH");
                        var kg = NlpContent.ExtractTextSection(r1, "# KNOWLEDGE GRAPH", null);
                        if (!string.IsNullOrWhiteSpace(al)) qAlgorithms = al;
                        if (!string.IsNullOrWhiteSpace(rt)) qResearchTopics = rt;
                        if (!string.IsNullOrWhiteSpace(ps)) qProblemSolving = ps;
                        if (!string.IsNullOrWhiteSpace(th)) qTheories = th;
                        if (!string.IsNullOrWhiteSpace(ar)) qArchitectures = ar;
                        if (!string.IsNullOrWhiteSpace(air)) qAiReport = air;
                        if (!string.IsNullOrWhiteSpace(cu)) qCurriculum = cu;
                        if (!string.IsNullOrWhiteSpace(kg)) qKnowledge = kg;
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio offline — used built-in quantum material.");
                }
                File.WriteAllText(Path.Combine(OutputDirectory, "quantum_algorithms.md"), qAlgorithms);
                File.WriteAllText(Path.Combine(OutputDirectory, "quantum_research_topics.txt"), qResearchTopics);
                File.WriteAllText(Path.Combine(OutputDirectory, "quantum_problem_solving.md"), qProblemSolving);
                File.WriteAllText(Path.Combine(OutputDirectory, "quantum_theories.md"), qTheories);
                File.WriteAllText(Path.Combine(OutputDirectory, "quantum_architectures.md"), qArchitectures);
                File.WriteAllText(Path.Combine(OutputDirectory, "quantum_ai_report.txt"), qAiReport);
                File.WriteAllText(Path.Combine(OutputDirectory, "quantum_curriculum.md"), qCurriculum);
                File.WriteAllText(Path.Combine(OutputDirectory, "quantum_knowledge_graph.md"), qKnowledge);

                // LM 2 — research paper -> .docx
                string qReport = QuantumContent.DefaultResearchMarkdown(qConcepts, qWords, avgAtt, avgMed, domKey);
                try
                {
                    Status?.Invoke("Writing the quantum report with LM Studio…");
                    using var c2 = new LmStudioClient(_config.LmStudioUrl);
                    string m2 = _config.Model;
                    if (string.IsNullOrWhiteSpace(m2)) m2 = await c2.GetFirstModelAsync(ct) ?? "";
                    if (!string.IsNullOrWhiteSpace(m2))
                    {
                        var p2 = QuantumPromptBuilder.BuildResearch(qSeed, seconds, qConcepts, avgAtt, avgMed, domKey, profile);
                        var r2 = RewritePromptBuilder.CleanReply(await c2.CompleteAsync(m2, p2.System, p2.User, ct));
                        if (!string.IsNullOrWhiteSpace(r2)) qReport = r2;
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio offline — used built-in quantum report.");
                }
                DocxArticleWriter.Write(qReport, Path.Combine(OutputDirectory, "quantum_computing_report.docx"), qFont);

                // LM 3 — 10-slide deck (accept only a full 10-slide LM deck)
                var qDeck = QuantumContent.DefaultDeck(qConcepts, qWords, avgAtt, avgMed, domKey);
                try
                {
                    Status?.Invoke("Building the 10-slide deck with LM Studio…");
                    using var c3 = new LmStudioClient(_config.LmStudioUrl);
                    string m3 = _config.Model;
                    if (string.IsNullOrWhiteSpace(m3)) m3 = await c3.GetFirstModelAsync(ct) ?? "";
                    if (!string.IsNullOrWhiteSpace(m3))
                    {
                        var p3 = QuantumPromptBuilder.BuildSlides(qConcepts, avgAtt, avgMed, domKey);
                        var r3 = RewritePromptBuilder.CleanReply(await c3.CompleteAsync(m3, p3.System, p3.User, ct));
                        var parsed = PptxArticleWriter.ParseSlides(r3, 10);
                        if (parsed.Count == 10) qDeck = parsed; // else keep the deterministic 10-slide deck
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio offline — used built-in 10-slide deck.");
                }
                PptxArticleWriter.Write(qDeck, Path.Combine(OutputDirectory, "quantum_computing_analysis.pptx"), qFont);

                Result?.Invoke(QuantumContent.Dashboard(qScores, qConcepts) + "\n\n" + qAlgorithms);
                Status?.Invoke($"Saved your quantum-computing package to {OutputDirectory}.");
                Completed?.Invoke(Path.Combine(OutputDirectory, "quantum_computing_report.docx"));
                return;
            }

            // ===== Reactive Machines: deterministic present-moment state + 3 LM calls (memory-less) =====
            if (reactive)
            {
                Directory.CreateDirectory(OutputDirectory);
                var rxWords = accumulator.Words;
                string rxSeed = accumulator.Seed();
                int rxDistinct = rxWords.Distinct(StringComparer.OrdinalIgnoreCase).Count();

                // recorded_eeg.csv + translated_eeg.txt (no timestamped copy — memory-less)
                File.WriteAllText(Path.Combine(OutputDirectory, "recorded_eeg.csv"), NlpContent.RecordedEegCsv(rxWords));
                string rxStats =
                    "# Translated EEG\n" +
                    $"recorded_seconds: {seconds}\n" +
                    $"words: {accumulator.Count}\n" +
                    $"distinct_words: {rxDistinct}\n" +
                    $"avg_attention: {avgAtt:0}\n" +
                    $"avg_meditation: {avgMed:0}\n" +
                    $"dominant_band: {domKey}\n\n";
                File.WriteAllText(Path.Combine(OutputDirectory, "translated_eeg.txt"), NlpContent.TranslatedText(rxStats, rxWords));

                // deterministic present-moment scores + derived CSVs
                var rxStates = ReactiveProfile.CurrentStates(avgAtt, avgMed, bandReadings, rxWords);
                var rxDash = ReactiveProfile.Dashboard(avgAtt, avgMed, bandReadings, rxWords);
                string rxDom = rxStates.OrderByDescending(s => s.Value).First().State;

                File.WriteAllText(Path.Combine(OutputDirectory, "current_state.csv"), ReactiveProfile.CurrentStateCsv(avgAtt, avgMed, bandReadings, rxWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "attention_response.csv"), ReactiveProfile.AttentionResponseCsv(avgAtt, bandReadings, rxWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "instant_decisions.csv"), ReactiveContent.InstantDecisionsCsv(rxStates, rxWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "opportunity_detection.csv"), ReactiveContent.OpportunityDetectionCsv(rxWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "stimulus_response.csv"), ReactiveContent.StimulusResponseCsv(rxWords, rxDom));
                File.WriteAllText(Path.Combine(OutputDirectory, "human_vs_reactive_machine.csv"), ReactiveContent.HumanVsReactiveCsv(rxWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "multi_input_reactions.csv"), ReactiveContent.MultiInputReactionsCsv(accumulator.Count));

                string rxFont = string.IsNullOrWhiteSpace(_config.Font) ? "Verdana" : _config.Font;

                // LM 1 — narratives (8 marked sections)
                string rxAnalysis = ReactiveContent.DefaultReactiveAnalysis(rxStates, rxWords);
                string rxSituation = ReactiveContent.DefaultSituationResponses(rxStates);
                string rxProblem = ReactiveContent.DefaultProblemSolver(rxWords);
                string rxResearch = ReactiveContent.DefaultResearchSuggestions(rxWords);
                string rxInnovation = ReactiveContent.DefaultInnovationIdeas(rxWords);
                string rxArchitecture = ReactiveContent.DefaultArchitectureConcepts(rxWords);
                string rxRobotics = ReactiveContent.DefaultRoboticsConcepts(rxWords);
                string rxActions = ReactiveContent.DefaultActionRecommendations(rxStates, rxWords);
                try
                {
                    Status?.Invoke("Reacting to the present input with LM Studio…");
                    using var c1 = new LmStudioClient(_config.LmStudioUrl);
                    string m1 = _config.Model;
                    if (string.IsNullOrWhiteSpace(m1)) m1 = await c1.GetFirstModelAsync(ct) ?? "";
                    if (!string.IsNullOrWhiteSpace(m1))
                    {
                        var p1 = ReactivePromptBuilder.BuildNarratives(rxSeed, rxDom, avgAtt, avgMed, domKey, profile);
                        var r1 = RewritePromptBuilder.CleanReply(await c1.CompleteAsync(m1, p1.System, p1.User, ct));
                        var an = NlpContent.ExtractTextSection(r1, "# REACTIVE ANALYSIS", "# SITUATION RESPONSES");
                        var si = NlpContent.ExtractTextSection(r1, "# SITUATION RESPONSES", "# PROBLEM SOLVER");
                        var pr = NlpContent.ExtractTextSection(r1, "# PROBLEM SOLVER", "# RESEARCH SUGGESTIONS");
                        var re = NlpContent.ExtractTextSection(r1, "# RESEARCH SUGGESTIONS", "# INNOVATION IDEAS");
                        var inn = NlpContent.ExtractTextSection(r1, "# INNOVATION IDEAS", "# ARCHITECTURE");
                        var arc = NlpContent.ExtractTextSection(r1, "# ARCHITECTURE", "# ROBOTICS");
                        var rob = NlpContent.ExtractTextSection(r1, "# ROBOTICS", "# ACTION RECOMMENDATIONS");
                        var act = NlpContent.ExtractTextSection(r1, "# ACTION RECOMMENDATIONS", null);
                        if (!string.IsNullOrWhiteSpace(an)) rxAnalysis = an;
                        if (!string.IsNullOrWhiteSpace(si)) rxSituation = si;
                        if (!string.IsNullOrWhiteSpace(pr)) rxProblem = pr;
                        if (!string.IsNullOrWhiteSpace(re)) rxResearch = re;
                        if (!string.IsNullOrWhiteSpace(inn)) rxInnovation = inn;
                        if (!string.IsNullOrWhiteSpace(arc)) rxArchitecture = arc;
                        if (!string.IsNullOrWhiteSpace(rob)) rxRobotics = rob;
                        if (!string.IsNullOrWhiteSpace(act)) rxActions = act;
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio offline — used built-in reactive responses.");
                }
                File.WriteAllText(Path.Combine(OutputDirectory, "reactive_analysis.txt"), rxAnalysis);
                File.WriteAllText(Path.Combine(OutputDirectory, "situation_responses.txt"), rxSituation);
                File.WriteAllText(Path.Combine(OutputDirectory, "problem_solver_report.txt"), rxProblem);
                File.WriteAllText(Path.Combine(OutputDirectory, "research_suggestions.txt"), rxResearch);
                File.WriteAllText(Path.Combine(OutputDirectory, "innovation_ideas.txt"), rxInnovation);
                File.WriteAllText(Path.Combine(OutputDirectory, "architecture_concepts.txt"), rxArchitecture);
                File.WriteAllText(Path.Combine(OutputDirectory, "robotics_concepts.txt"), rxRobotics);
                File.WriteAllText(Path.Combine(OutputDirectory, "action_recommendations.txt"), rxActions);

                // LM 2 — research paper -> .docx
                string rxReport = ReactiveContent.DefaultResearchMarkdown(rxStates, rxWords, avgAtt, avgMed, domKey);
                try
                {
                    Status?.Invoke("Writing the reactive machine report with LM Studio…");
                    using var c2 = new LmStudioClient(_config.LmStudioUrl);
                    string m2 = _config.Model;
                    if (string.IsNullOrWhiteSpace(m2)) m2 = await c2.GetFirstModelAsync(ct) ?? "";
                    if (!string.IsNullOrWhiteSpace(m2))
                    {
                        var p2 = ReactivePromptBuilder.BuildResearch(rxSeed, seconds, rxDom, avgAtt, avgMed, domKey, profile);
                        var r2 = RewritePromptBuilder.CleanReply(await c2.CompleteAsync(m2, p2.System, p2.User, ct));
                        if (!string.IsNullOrWhiteSpace(r2)) rxReport = r2;
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio offline — used built-in reactive report.");
                }
                DocxArticleWriter.Write(rxReport, Path.Combine(OutputDirectory, "reactive_machine_report.docx"), rxFont);

                // LM 3 — 10-slide deck (accept only a full 10-slide LM deck)
                var rxDeck = ReactiveContent.DefaultDeck(rxStates, rxWords, avgAtt, avgMed, domKey);
                try
                {
                    Status?.Invoke("Building the 10-slide deck with LM Studio…");
                    using var c3 = new LmStudioClient(_config.LmStudioUrl);
                    string m3 = _config.Model;
                    if (string.IsNullOrWhiteSpace(m3)) m3 = await c3.GetFirstModelAsync(ct) ?? "";
                    if (!string.IsNullOrWhiteSpace(m3))
                    {
                        var p3 = ReactivePromptBuilder.BuildSlides(rxDom, avgAtt, avgMed, domKey);
                        var r3 = RewritePromptBuilder.CleanReply(await c3.CompleteAsync(m3, p3.System, p3.User, ct));
                        var parsed = PptxArticleWriter.ParseSlides(r3, 10);
                        if (parsed.Count == 10) rxDeck = parsed; // else keep the deterministic 10-slide deck
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio offline — used built-in 10-slide deck.");
                }
                PptxArticleWriter.Write(rxDeck, Path.Combine(OutputDirectory, "reactive_machine_analysis.pptx"), rxFont);

                Result?.Invoke(ReactiveContent.Dashboard(rxDash, rxStates) + "\n\n" + rxAnalysis);
                Status?.Invoke($"Saved your reactive-machine package to {OutputDirectory}.");
                Completed?.Invoke(Path.Combine(OutputDirectory, "reactive_machine_report.docx"));
                return;
            }

            // ===== Reasoning: deterministic score-sets + trends + network + 3 LM calls =====
            if (reasoning)
            {
                Directory.CreateDirectory(OutputDirectory);
                var rsWords = accumulator.Words;
                string rsSeed = accumulator.Seed();
                int rsDistinct = rsWords.Distinct(StringComparer.OrdinalIgnoreCase).Count();
                var dataDir = Path.Combine(AppContext.BaseDirectory, "data");

                // recorded_eeg.csv + translated_eeg.txt
                File.WriteAllText(Path.Combine(OutputDirectory, "recorded_eeg.csv"), NlpContent.RecordedEegCsv(rsWords));
                string rsStats =
                    "# Translated EEG\n" +
                    $"recorded_seconds: {seconds}\n" +
                    $"words: {accumulator.Count}\n" +
                    $"distinct_words: {rsDistinct}\n" +
                    $"avg_attention: {avgAtt:0}\n" +
                    $"avg_meditation: {avgMed:0}\n" +
                    $"dominant_band: {domKey}\n\n";
                File.WriteAllText(Path.Combine(OutputDirectory, "translated_eeg.txt"), NlpContent.TranslatedText(rsStats, rsWords));

                // deterministic score-sets + subject ranking
                var rsSubjects = ReasoningSubjects.DetectFromFile(dataDir, rsWords);
                var rsDash = ReasoningProfile.Dashboard(avgAtt, avgMed, bandReadings, rsWords);
                string rsTop = rsSubjects.Count > 0 ? rsSubjects[0].Subject : "General";

                File.WriteAllText(Path.Combine(OutputDirectory, "logical_reasoning.csv"), ReasoningProfile.LogicalReasoningCsv(avgAtt, bandReadings, rsWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "problem_solving_profile.csv"), ReasoningProfile.ProblemSolvingProfileCsv(avgAtt, avgMed, bandReadings, rsWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "inference_analysis.csv"), ReasoningProfile.InferenceAnalysisCsv(avgAtt, bandReadings, rsWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "reasoning_profile.csv"), ReasoningProfile.ReasoningProfileCsv(avgAtt, avgMed, bandReadings, rsWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "scientific_reasoning.csv"), ReasoningProfile.ScientificReasoningCsv(avgAtt, bandReadings, rsWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "engineering_reasoning.csv"), ReasoningProfile.EngineeringReasoningCsv(avgAtt, bandReadings, rsWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "mathematical_reasoning.csv"), ReasoningProfile.MathematicalReasoningCsv(avgAtt, bandReadings, rsWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "innovation_analysis.csv"), ReasoningProfile.InnovationAnalysisCsv(avgAtt, bandReadings, rsWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "reasoning_chains.csv"), ReasoningProfile.ReasoningChainsCsv(avgAtt, bandReadings, rsWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "hypotheses.csv"), ReasoningContent.HypothesesCsv(rsWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "artificial_reasoning_comparison.csv"), ReasoningContent.ArtificialComparisonCsv(rsDash));
                File.WriteAllText(Path.Combine(OutputDirectory, "subject_reasoning_scores.csv"), ReasoningContent.SubjectScoresCsv(rsSubjects));

                // trends (current + priors) + network (csv_files) + history
                var rsSessions = new List<ReasoningScan.ReasoningSession>
                {
                    new("current", DateTime.Now, rsDash[0].Value, rsDash[3].Value, rsDash[2].Value),
                };
                rsSessions.AddRange(ReasoningScan.Scan(OutputDirectory));
                File.WriteAllText(Path.Combine(OutputDirectory, "reasoning_trends.csv"), ReasoningScan.TrendsCsv(rsSessions));
                File.WriteAllText(Path.Combine(OutputDirectory, "reasoning_network_rankings.csv"), ReasoningScan.NetworkRankingsCsv(OutputDirectory));
                ReasoningProfile.AppendHistory(Path.Combine(OutputDirectory, "reasoning_history.csv"), avgAtt, avgMed, bandReadings, rsWords, rsTop);

                // save this run as a timestamped recording so future runs can scan it
                var rsStamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                File.WriteAllText(Path.Combine(OutputDirectory, $"recorded_eeg_{rsStamp}.csv"), NlpContent.RecordedEegCsv(rsWords));

                string rsFont = string.IsNullOrWhiteSpace(_config.Font) ? "Verdana" : _config.Font;

                // LM 1 — narratives (4 marked sections)
                string rsArgument = ReasoningContent.DefaultArgumentAnalysis(rsWords);
                string rsDecision = ReasoningContent.DefaultDecisionPathways(rsWords);
                string rsCritical = ReasoningContent.DefaultCriticalThinking(rsWords);
                string rsForecast = ReasoningContent.DefaultFutureForecast(rsSubjects);
                try
                {
                    Status?.Invoke("Analyzing reasoning with LM Studio…");
                    using var c1 = new LmStudioClient(_config.LmStudioUrl);
                    string m1 = _config.Model;
                    if (string.IsNullOrWhiteSpace(m1)) m1 = await c1.GetFirstModelAsync(ct) ?? "";
                    if (!string.IsNullOrWhiteSpace(m1))
                    {
                        var p1 = ReasoningPromptBuilder.BuildNarratives(rsSeed, rsSubjects, avgAtt, avgMed, domKey, profile);
                        var r1 = RewritePromptBuilder.CleanReply(await c1.CompleteAsync(m1, p1.System, p1.User, ct));
                        var ar = NlpContent.ExtractTextSection(r1, "# ARGUMENT", "# DECISION PATHWAYS");
                        var de = NlpContent.ExtractTextSection(r1, "# DECISION PATHWAYS", "# CRITICAL THINKING");
                        var cr = NlpContent.ExtractTextSection(r1, "# CRITICAL THINKING", "# FUTURE FORECAST");
                        var fo = NlpContent.ExtractTextSection(r1, "# FUTURE FORECAST", null);
                        if (!string.IsNullOrWhiteSpace(ar)) rsArgument = ar;
                        if (!string.IsNullOrWhiteSpace(de)) rsDecision = de;
                        if (!string.IsNullOrWhiteSpace(cr)) rsCritical = cr;
                        if (!string.IsNullOrWhiteSpace(fo)) rsForecast = fo;
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio offline — used built-in reasoning narratives.");
                }
                File.WriteAllText(Path.Combine(OutputDirectory, "argument_analysis.txt"), rsArgument);
                File.WriteAllText(Path.Combine(OutputDirectory, "decision_pathways.md"), rsDecision);
                File.WriteAllText(Path.Combine(OutputDirectory, "critical_thinking_report.txt"), rsCritical);
                File.WriteAllText(Path.Combine(OutputDirectory, "future_reasoning_forecast.txt"), rsForecast);

                // LM 2 — research paper -> .docx
                string rsReport = ReasoningContent.DefaultResearchMarkdown(rsSubjects, rsWords, avgAtt, avgMed, domKey);
                try
                {
                    Status?.Invoke("Writing the reasoning report with LM Studio…");
                    using var c2 = new LmStudioClient(_config.LmStudioUrl);
                    string m2 = _config.Model;
                    if (string.IsNullOrWhiteSpace(m2)) m2 = await c2.GetFirstModelAsync(ct) ?? "";
                    if (!string.IsNullOrWhiteSpace(m2))
                    {
                        var p2 = ReasoningPromptBuilder.BuildResearch(rsSeed, seconds, rsSubjects, avgAtt, avgMed, domKey, profile);
                        var r2 = RewritePromptBuilder.CleanReply(await c2.CompleteAsync(m2, p2.System, p2.User, ct));
                        if (!string.IsNullOrWhiteSpace(r2)) rsReport = r2;
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio offline — used built-in reasoning report.");
                }
                DocxArticleWriter.Write(rsReport, Path.Combine(OutputDirectory, "reasoning_analysis_report.docx"), rsFont);

                // LM 3 — 10-slide deck (accept only a full 10-slide LM deck)
                var rsDeck = ReasoningContent.DefaultDeck(rsSubjects, rsWords, avgAtt, avgMed, domKey);
                try
                {
                    Status?.Invoke("Building the 10-slide deck with LM Studio…");
                    using var c3 = new LmStudioClient(_config.LmStudioUrl);
                    string m3 = _config.Model;
                    if (string.IsNullOrWhiteSpace(m3)) m3 = await c3.GetFirstModelAsync(ct) ?? "";
                    if (!string.IsNullOrWhiteSpace(m3))
                    {
                        var p3 = ReasoningPromptBuilder.BuildSlides(rsSubjects, avgAtt, avgMed, domKey);
                        var r3 = RewritePromptBuilder.CleanReply(await c3.CompleteAsync(m3, p3.System, p3.User, ct));
                        var parsed = PptxArticleWriter.ParseSlides(r3, 10);
                        if (parsed.Count == 10) rsDeck = parsed; // else keep the deterministic 10-slide deck
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio offline — used built-in 10-slide deck.");
                }
                PptxArticleWriter.Write(rsDeck, Path.Combine(OutputDirectory, "reasoning_analysis.pptx"), rsFont);

                Result?.Invoke(ReasoningContent.Dashboard(rsDash, rsSubjects) + "\n\n" + rsArgument);
                Status?.Invoke($"Saved your reasoning package to {OutputDirectory}.");
                Completed?.Invoke(Path.Combine(OutputDirectory, "reasoning_analysis_report.docx"));
                return;
            }

            // ===== Multi-Agent System: deterministic team + trends + network + 3 LM calls =====
            if (mas)
            {
                Directory.CreateDirectory(OutputDirectory);
                var maWords = accumulator.Words;
                string maSeed = accumulator.Seed();
                int maDistinct = maWords.Distinct(StringComparer.OrdinalIgnoreCase).Count();
                var dataDir = Path.Combine(AppContext.BaseDirectory, "data");

                // recorded_eeg.csv + translated_eeg.txt
                File.WriteAllText(Path.Combine(OutputDirectory, "recorded_eeg.csv"), NlpContent.RecordedEegCsv(maWords));
                string maStats =
                    "# Translated EEG\n" +
                    $"recorded_seconds: {seconds}\n" +
                    $"words: {accumulator.Count}\n" +
                    $"distinct_words: {maDistinct}\n" +
                    $"avg_attention: {avgAtt:0}\n" +
                    $"avg_meditation: {avgMed:0}\n" +
                    $"dominant_band: {domKey}\n\n";
                File.WriteAllText(Path.Combine(OutputDirectory, "translated_eeg.txt"), NlpContent.TranslatedText(maStats, maWords));

                // deterministic team + domain ranking
                var maAgents = MasTeam.Roster();
                var maDomains = MasDomains.DetectFromFile(dataDir, maWords);
                var maMetrics = MasTeam.CoordinationMetrics(avgAtt, avgMed, bandReadings, maWords);
                string maTopDomain = maDomains.Count > 0 ? maDomains[0].Domain : "General";

                File.WriteAllText(Path.Combine(OutputDirectory, "mission_brief.csv"), MasContent.MissionBriefCsv(maWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "agent_roster.csv"), MasTeam.RosterCsv(maAgents));
                File.WriteAllText(Path.Combine(OutputDirectory, "task_assignments.csv"), MasTeam.TaskAssignmentsCsv(maAgents));
                File.WriteAllText(Path.Combine(OutputDirectory, "collaboration_matrix.csv"), MasTeam.CollaborationMatrixCsv(maAgents, avgAtt, bandReadings, maWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "agent_performance.csv"), MasTeam.AgentPerformanceCsv(avgAtt, avgMed, bandReadings, maWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "coordination_metrics.csv"), MasTeam.CoordinationMetricsCsv(avgAtt, avgMed, bandReadings, maWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "consensus_analysis.csv"), MasTeam.ConsensusAnalysisCsv(avgAtt, avgMed, bandReadings, maWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "communication_log.csv"), MasTeam.CommunicationLogCsv(maAgents));
                File.WriteAllText(Path.Combine(OutputDirectory, "team_domains.csv"), MasContent.DomainScoresCsv(maDomains));

                // trends (current + priors) + network (csv_files) + history
                var maSessions = new List<MasScan.MasSession>
                {
                    new("current", DateTime.Now, maMetrics[0].Value, maMetrics[2].Value, maMetrics[3].Value),
                };
                maSessions.AddRange(MasScan.Scan(OutputDirectory));
                File.WriteAllText(Path.Combine(OutputDirectory, "mas_trends.csv"), MasScan.TrendsCsv(maSessions));
                File.WriteAllText(Path.Combine(OutputDirectory, "mas_network_rankings.csv"), MasScan.NetworkRankingsCsv(OutputDirectory));
                MasTeam.AppendHistory(Path.Combine(OutputDirectory, "mas_history.csv"), avgAtt, avgMed, bandReadings, maWords, maTopDomain);

                // save this run as a timestamped recording so future runs can scan it
                var maStamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                File.WriteAllText(Path.Combine(OutputDirectory, $"recorded_eeg_{maStamp}.csv"), NlpContent.RecordedEegCsv(maWords));

                string maFont = string.IsNullOrWhiteSpace(_config.Font) ? "Verdana" : _config.Font;

                // LM 1 — 10 agent contributions (10 marked sections # AGENT 01 … # AGENT 10)
                var maContribs = new List<string>(MasContent.DefaultAgentContributions(maAgents, maWords));
                try
                {
                    Status?.Invoke("Running the 10-agent team with LM Studio…");
                    using var c1 = new LmStudioClient(_config.LmStudioUrl);
                    string m1 = _config.Model;
                    if (string.IsNullOrWhiteSpace(m1)) m1 = await c1.GetFirstModelAsync(ct) ?? "";
                    if (!string.IsNullOrWhiteSpace(m1))
                    {
                        var p1 = MasPromptBuilder.BuildAgents(maSeed, maAgents, MasContent.MissionLine(maWords), avgAtt, avgMed, domKey, profile);
                        var r1 = RewritePromptBuilder.CleanReply(await c1.CompleteAsync(m1, p1.System, p1.User, ct));
                        for (int i = 0; i < maAgents.Count; i++)
                        {
                            string startMarker = $"# AGENT {maAgents[i].Index:00}";
                            string? endMarker = i + 1 < maAgents.Count ? $"# AGENT {maAgents[i + 1].Index:00}" : null;
                            var section = NlpContent.ExtractTextSection(r1, startMarker, endMarker);
                            if (!string.IsNullOrWhiteSpace(section)) maContribs[i] = section;
                        }
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio offline — used built-in agent contributions.");
                }
                var maTranscript = new System.Text.StringBuilder();
                for (int i = 0; i < maAgents.Count; i++)
                {
                    string file = $"agent_{maAgents[i].Index:00}_{maAgents[i].Role.ToLowerInvariant()}.txt";
                    File.WriteAllText(Path.Combine(OutputDirectory, file), maContribs[i]);
                    maTranscript.AppendLine($"# AGENT {maAgents[i].Index:00}: {maAgents[i].Role}");
                    maTranscript.AppendLine(maContribs[i]);
                    maTranscript.AppendLine();
                }
                File.WriteAllText(Path.Combine(OutputDirectory, "agent_transcripts.md"), maTranscript.ToString());

                // LM 2 — mission report -> .docx
                string maReport = MasContent.DefaultResearchMarkdown(maAgents, maMetrics, maWords, avgAtt, avgMed, domKey);
                try
                {
                    Status?.Invoke("Writing the mission report with LM Studio…");
                    using var c2 = new LmStudioClient(_config.LmStudioUrl);
                    string m2 = _config.Model;
                    if (string.IsNullOrWhiteSpace(m2)) m2 = await c2.GetFirstModelAsync(ct) ?? "";
                    if (!string.IsNullOrWhiteSpace(m2))
                    {
                        var p2 = MasPromptBuilder.BuildResearch(maSeed, seconds, maAgents, maMetrics, avgAtt, avgMed, domKey, profile);
                        var r2 = RewritePromptBuilder.CleanReply(await c2.CompleteAsync(m2, p2.System, p2.User, ct));
                        if (!string.IsNullOrWhiteSpace(r2)) maReport = r2;
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio offline — used built-in mission report.");
                }
                DocxArticleWriter.Write(maReport, Path.Combine(OutputDirectory, "mission_report.docx"), maFont);

                // LM 3 — 10-slide deck (accept only a full 10-slide LM deck)
                var maDeck = MasContent.DefaultDeck(maAgents, maMetrics, maWords, avgAtt, avgMed, domKey);
                try
                {
                    Status?.Invoke("Building the 10-slide deck with LM Studio…");
                    using var c3 = new LmStudioClient(_config.LmStudioUrl);
                    string m3 = _config.Model;
                    if (string.IsNullOrWhiteSpace(m3)) m3 = await c3.GetFirstModelAsync(ct) ?? "";
                    if (!string.IsNullOrWhiteSpace(m3))
                    {
                        var p3 = MasPromptBuilder.BuildSlides(maAgents, maMetrics, avgAtt, avgMed, domKey);
                        var r3 = RewritePromptBuilder.CleanReply(await c3.CompleteAsync(m3, p3.System, p3.User, ct));
                        var parsed = PptxArticleWriter.ParseSlides(r3, 10);
                        if (parsed.Count == 10) maDeck = parsed; // else keep the deterministic 10-slide deck
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio offline — used built-in 10-slide deck.");
                }
                PptxArticleWriter.Write(maDeck, Path.Combine(OutputDirectory, "mas_analysis.pptx"), maFont);

                Result?.Invoke(MasContent.Dashboard(maMetrics, maAgents, maDomains) + "\n\n" + maContribs[0]);
                Status?.Invoke($"Saved your multi-agent package to {OutputDirectory}.");
                Completed?.Invoke(Path.Combine(OutputDirectory, "mission_report.docx"));
                return;
            }

            // ===== Reinforcement Learning: deterministic RL model + trends + network + 3 LM calls =====
            if (rl)
            {
                Directory.CreateDirectory(OutputDirectory);
                var rlWords = accumulator.Words;
                string rlSeed = accumulator.Seed();
                int rlDistinct = rlWords.Distinct(StringComparer.OrdinalIgnoreCase).Count();
                var dataDir = Path.Combine(AppContext.BaseDirectory, "data");

                // recorded_eeg.csv + translated_eeg.txt
                File.WriteAllText(Path.Combine(OutputDirectory, "recorded_eeg.csv"), NlpContent.RecordedEegCsv(rlWords));
                string rlStats =
                    "# Translated EEG\n" +
                    $"recorded_seconds: {seconds}\n" +
                    $"words: {accumulator.Count}\n" +
                    $"distinct_words: {rlDistinct}\n" +
                    $"signal_quality: {(rlWords.Count > 0 ? "good" : "no signal")}\n" +
                    $"avg_attention: {avgAtt:0}\n" +
                    $"avg_meditation: {avgMed:0}\n" +
                    $"dominant_band: {domKey}\n\n";
                File.WriteAllText(Path.Combine(OutputDirectory, "translated_eeg.txt"), NlpContent.TranslatedText(rlStats, rlWords));

                // deterministic score-sets + goal ranking
                var rlGoals = RlGoals.DetectFromFile(dataDir, rlWords);
                var rlDash = RlProfile.Dashboard(avgAtt, avgMed, bandReadings, rlWords);
                var rlRewards = RlProfile.RewardScores(avgAtt, avgMed, bandReadings, rlWords);
                var rlStates = RlProfile.BrainStates(avgAtt, avgMed, bandReadings, rlWords);
                var rlActions = RlProfile.BrainActions(avgAtt, avgMed, bandReadings, rlWords);
                var rlScores = RlProfile.RlScores(avgAtt, avgMed, bandReadings, rlWords);
                string rlTopGoal = rlGoals.Count > 0 ? rlGoals[0].Goal : "General";

                File.WriteAllText(Path.Combine(OutputDirectory, "brain_states.csv"), RlProfile.BrainStatesCsv(avgAtt, avgMed, bandReadings, rlWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "brain_actions.csv"), RlProfile.BrainActionsCsv(avgAtt, avgMed, bandReadings, rlWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "reward_scores.csv"), RlProfile.RewardScoresCsv(avgAtt, avgMed, bandReadings, rlWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "brain_policy.csv"), RlProfile.BrainPolicyCsv(avgAtt, avgMed, bandReadings, rlWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "decision_analysis.csv"), RlProfile.DecisionAnalysisCsv(avgAtt, avgMed, bandReadings, rlWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "goal_alignment.csv"), RlContent.GoalAlignmentCsv(rlGoals));
                File.WriteAllText(Path.Combine(OutputDirectory, "reward_map.csv"), RlContent.RewardMapCsv(rlWords, rlRewards));
                File.WriteAllText(Path.Combine(OutputDirectory, "exploration_exploitation.csv"), RlProfile.ExplorationExploitationCsv(avgAtt, avgMed, bandReadings, rlWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "learning_episodes.csv"), RlContent.LearningEpisodesCsv(rlWords, rlStates, rlActions, rlRewards));
                File.WriteAllText(Path.Combine(OutputDirectory, "brain_agent_profile.csv"), RlContent.BrainAgentProfileCsv(rlScores, rlRewards));
                File.WriteAllText(Path.Combine(OutputDirectory, "rl_scores.csv"), RlProfile.RlScoresCsv(avgAtt, avgMed, bandReadings, rlWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "simulation_results.csv"), RlContent.SimulationResultsCsv(rlWords, rlRewards));

                // trends (current + priors) + network (csv_files) + history
                var rlSessions = new List<RlScan.RlSession>
                {
                    new("current", DateTime.Now, rlDash[1].Value, rlDash[2].Value, rlDash[6].Value),
                };
                rlSessions.AddRange(RlScan.Scan(OutputDirectory));
                File.WriteAllText(Path.Combine(OutputDirectory, "learning_trends.csv"), RlScan.TrendsCsv(rlSessions));
                File.WriteAllText(Path.Combine(OutputDirectory, "multi_agent_rankings.csv"), RlScan.NetworkRankingsCsv(OutputDirectory));
                RlProfile.AppendHistory(Path.Combine(OutputDirectory, "reinforcement_history.csv"), avgAtt, avgMed, bandReadings, rlWords, rlTopGoal);

                // save this run as a timestamped recording so future runs can scan it
                var rlStamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                File.WriteAllText(Path.Combine(OutputDirectory, $"recorded_eeg_{rlStamp}.csv"), NlpContent.RecordedEegCsv(rlWords));

                string rlFont = string.IsNullOrWhiteSpace(_config.Font) ? "Verdana" : _config.Font;

                // LM 1 — future learning strategy -> .txt
                string rlStrategy = RlContent.DefaultFutureStrategy(rlGoals, rlWords);
                try
                {
                    Status?.Invoke("Generating the future learning strategy with LM Studio…");
                    using var c1 = new LmStudioClient(_config.LmStudioUrl);
                    string m1 = _config.Model;
                    if (string.IsNullOrWhiteSpace(m1)) m1 = await c1.GetFirstModelAsync(ct) ?? "";
                    if (!string.IsNullOrWhiteSpace(m1))
                    {
                        var p1 = RlPromptBuilder.BuildStrategy(rlSeed, rlGoals, rlDash, avgAtt, avgMed, domKey, profile);
                        var r1 = RewritePromptBuilder.CleanReply(await c1.CompleteAsync(m1, p1.System, p1.User, ct));
                        if (!string.IsNullOrWhiteSpace(r1)) rlStrategy = r1;
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio offline — used built-in learning strategy.");
                }
                File.WriteAllText(Path.Combine(OutputDirectory, "future_learning_strategy.txt"), rlStrategy);

                // LM 2 — research report -> .docx
                string rlReport = RlContent.DefaultResearchMarkdown(rlGoals, rlDash, rlWords, avgAtt, avgMed, domKey);
                try
                {
                    Status?.Invoke("Writing the reinforcement-learning report with LM Studio…");
                    using var c2 = new LmStudioClient(_config.LmStudioUrl);
                    string m2 = _config.Model;
                    if (string.IsNullOrWhiteSpace(m2)) m2 = await c2.GetFirstModelAsync(ct) ?? "";
                    if (!string.IsNullOrWhiteSpace(m2))
                    {
                        var p2 = RlPromptBuilder.BuildResearch(rlSeed, seconds, rlGoals, rlDash, avgAtt, avgMed, domKey, profile);
                        var r2 = RewritePromptBuilder.CleanReply(await c2.CompleteAsync(m2, p2.System, p2.User, ct));
                        if (!string.IsNullOrWhiteSpace(r2)) rlReport = r2;
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio offline — used built-in reinforcement-learning report.");
                }
                DocxArticleWriter.Write(rlReport, Path.Combine(OutputDirectory, "reinforcement_learning_report.docx"), rlFont);

                // LM 3 — 10-slide deck (accept only a full 10-slide LM deck)
                var rlDeck = RlContent.DefaultDeck(rlGoals, rlDash, rlWords, avgAtt, avgMed, domKey);
                try
                {
                    Status?.Invoke("Building the 10-slide deck with LM Studio…");
                    using var c3 = new LmStudioClient(_config.LmStudioUrl);
                    string m3 = _config.Model;
                    if (string.IsNullOrWhiteSpace(m3)) m3 = await c3.GetFirstModelAsync(ct) ?? "";
                    if (!string.IsNullOrWhiteSpace(m3))
                    {
                        var p3 = RlPromptBuilder.BuildSlides(rlGoals, rlDash, avgAtt, avgMed, domKey);
                        var r3 = RewritePromptBuilder.CleanReply(await c3.CompleteAsync(m3, p3.System, p3.User, ct));
                        var parsed = PptxArticleWriter.ParseSlides(r3, 10);
                        if (parsed.Count == 10) rlDeck = parsed; // else keep the deterministic 10-slide deck
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio offline — used built-in 10-slide deck.");
                }
                PptxArticleWriter.Write(rlDeck, Path.Combine(OutputDirectory, "reinforcement_learning_analysis.pptx"), rlFont);

                Result?.Invoke(RlContent.Dashboard(rlDash, rlGoals) + "\n\n" + rlStrategy);
                Status?.Invoke($"Saved your reinforcement-learning package to {OutputDirectory}.");
                Completed?.Invoke(Path.Combine(OutputDirectory, "reinforcement_learning_report.docx"));
                return;
            }

            // ===== Robot: deterministic robot brain + optional vision + 4 LM calls =====
            if (robot)
            {
                Directory.CreateDirectory(OutputDirectory);
                var rbWords = accumulator.Words;
                string rbSeed = accumulator.Seed();
                int rbDistinct = rbWords.Distinct(StringComparer.OrdinalIgnoreCase).Count();
                var dataDir = Path.Combine(AppContext.BaseDirectory, "data");

                // recorded_eeg.csv + translated_eeg.txt
                File.WriteAllText(Path.Combine(OutputDirectory, "recorded_eeg.csv"), NlpContent.RecordedEegCsv(rbWords));
                string rbStats =
                    "# Translated EEG\n" +
                    $"recorded_seconds: {seconds}\n" +
                    $"words: {accumulator.Count}\n" +
                    $"distinct_words: {rbDistinct}\n" +
                    $"signal_quality: {(rbWords.Count > 0 ? "good" : "no signal")}\n" +
                    $"avg_attention: {avgAtt:0}\n" +
                    $"avg_meditation: {avgMed:0}\n" +
                    $"dominant_band: {domKey}\n\n";
                File.WriteAllText(Path.Combine(OutputDirectory, "translated_eeg.txt"), NlpContent.TranslatedText(rbStats, rbWords));

                // deterministic robot brain
                var rbActions = RobotActions.DetectFromFile(dataDir);
                var rbDash = RobotProfile.Dashboard(avgAtt, avgMed, bandReadings, rbWords);
                var rbState = RobotProfile.RobotState(avgAtt, avgMed, bandReadings, rbWords);
                var rbPersonality = RobotProfile.Personality(avgAtt, avgMed, bandReadings, rbWords);
                var rbSkills = RobotProfile.Skills(avgAtt, avgMed, bandReadings, rbWords);
                string rbTopTrait = rbPersonality.OrderByDescending(p => p.Value).First().Trait;

                File.WriteAllText(Path.Combine(OutputDirectory, "robot_state.csv"), RobotProfile.RobotStateCsv(avgAtt, avgMed, bandReadings, rbWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "navigation_plan.csv"), RobotProfile.NavigationPlanCsv(avgAtt, avgMed, bandReadings, rbWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "robot_commands.csv"), rbActions.CommandsCsv(rbWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "environment_map.json"), RobotContent.EnvironmentMapJson(rbWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "robot_memory.csv"), RobotContent.RobotMemoryCsv(rbWords, rbState));
                File.WriteAllText(Path.Combine(OutputDirectory, "robot_personality.csv"), RobotProfile.PersonalityCsv(avgAtt, avgMed, bandReadings, rbWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "robot_skills.csv"), RobotProfile.SkillsCsv(avgAtt, avgMed, bandReadings, rbWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "simulation_results.csv"), RobotContent.SimulationResultsCsv(rbDash, rbWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "brain_robot_actions.csv"), rbActions.BrainActionsCsv(rbWords));

                // network coordination + history
                File.WriteAllText(Path.Combine(OutputDirectory, "robot_network_analysis.csv"), RobotScan.NetworkAnalysisCsv(OutputDirectory));
                RobotProfile.AppendHistory(Path.Combine(OutputDirectory, "robot_history.csv"), avgAtt, avgMed, bandReadings, rbWords, rbTopTrait);

                // save this run as a timestamped recording so future runs can scan it
                var rbStamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                File.WriteAllText(Path.Combine(OutputDirectory, $"recorded_eeg_{rbStamp}.csv"), NlpContent.RecordedEegCsv(rbWords));

                string rbFont = string.IsNullOrWhiteSpace(_config.Font) ? "Verdana" : _config.Font;

                // object recognition (optional robot_camera_images/ folder, LM vision, capped at 3)
                string rbObjectCsv = RobotContent.ObjectRecognitionFallbackCsv(rbWords);
                try
                {
                    var camDir = Path.Combine(OutputDirectory, "robot_camera_images");
                    if (Directory.Exists(camDir))
                    {
                        var imgs = Directory.EnumerateFiles(camDir)
                            .Where(f => new[] { ".png", ".jpg", ".jpeg", ".gif", ".webp" }.Contains(Path.GetExtension(f).ToLowerInvariant()))
                            .Take(3).ToList();
                        if (imgs.Count > 0)
                        {
                            Status?.Invoke($"Recognizing objects in {imgs.Count} image(s) with LM Studio vision…");
                            using var ic = new LmStudioClient(_config.LmStudioUrl);
                            string im = _config.Model;
                            if (string.IsNullOrWhiteSpace(im)) im = await ic.GetFirstModelAsync(ct) ?? "";
                            var sb = new System.Text.StringBuilder("source,object,type,confidence\n");
                            foreach (var img in imgs)
                            {
                                string objName = "object", objType = "object";
                                if (!string.IsNullOrWhiteSpace(im))
                                {
                                    var ip = RobotPromptBuilder.BuildVision(rbSeed);
                                    var bytes = await File.ReadAllBytesAsync(img, ct);
                                    var visionReply = RewritePromptBuilder.CleanReply(
                                        await ic.CompleteWithImageAsync(im, ip.System, ip.User, bytes, RobotContent.MimeFor(img), ct));
                                    var firstLine = visionReply.Replace("\r", "").Split('\n').FirstOrDefault(l => l.Trim().Length > 0)?.Trim() ?? "";
                                    var pieces = firstLine.Split('-', 2);
                                    if (pieces.Length == 2) { objName = pieces[0].Replace(",", " ").Trim(); objType = pieces[1].Replace(",", " ").Trim(); }
                                    else if (firstLine.Length > 0) { objName = firstLine.Replace(",", " ").Trim(); objType = "object"; }
                                    if (objName.Length == 0) objName = "object";
                                    if (objName.Length > 60) objName = objName[..60];
                                    if (objType.Length > 40) objType = objType[..40];
                                }
                                sb.AppendLine($"{Path.GetFileName(img)},{objName},{objType},70");
                            }
                            rbObjectCsv = sb.ToString();
                        }
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio vision unavailable — used the deterministic object list.");
                }
                File.WriteAllText(Path.Combine(OutputDirectory, "object_recognition.csv"), rbObjectCsv);

                // LM 1 — narratives (4 marked sections)
                string rbBrain = RobotContent.DefaultBrainState(rbWords);
                string rbTasks = RobotContent.DefaultAutonomousTasks(rbWords);
                string rbChat = RobotContent.DefaultChatLog(rbWords);
                string rbEvolution = RobotContent.DefaultEvolution(rbWords);
                try
                {
                    Status?.Invoke("Generating the robot brain with LM Studio…");
                    using var c1 = new LmStudioClient(_config.LmStudioUrl);
                    string m1 = _config.Model;
                    if (string.IsNullOrWhiteSpace(m1)) m1 = await c1.GetFirstModelAsync(ct) ?? "";
                    if (!string.IsNullOrWhiteSpace(m1))
                    {
                        var p1 = RobotPromptBuilder.BuildNarratives(rbSeed, avgAtt, avgMed, domKey, profile);
                        var r1 = RewritePromptBuilder.CleanReply(await c1.CompleteAsync(m1, p1.System, p1.User, ct));
                        var bs = NlpContent.ExtractTextSection(r1, "# BRAIN STATE", "# AUTONOMOUS TASKS");
                        var at = NlpContent.ExtractTextSection(r1, "# AUTONOMOUS TASKS", "# CHAT LOG");
                        var cl = NlpContent.ExtractTextSection(r1, "# CHAT LOG", "# EVOLUTION");
                        var ev = NlpContent.ExtractTextSection(r1, "# EVOLUTION", null);
                        if (!string.IsNullOrWhiteSpace(bs)) rbBrain = bs;
                        if (!string.IsNullOrWhiteSpace(at)) rbTasks = at;
                        if (!string.IsNullOrWhiteSpace(cl)) rbChat = cl;
                        if (!string.IsNullOrWhiteSpace(ev)) rbEvolution = ev;
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio offline — used built-in robot narratives.");
                }
                File.WriteAllText(Path.Combine(OutputDirectory, "robot_brain_state.txt"), rbBrain);
                File.WriteAllText(Path.Combine(OutputDirectory, "autonomous_tasks.md"), rbTasks);
                File.WriteAllText(Path.Combine(OutputDirectory, "robot_chat_log.txt"), rbChat);
                File.WriteAllText(Path.Combine(OutputDirectory, "robot_evolution_report.txt"), rbEvolution);

                // LM 2 — analysis report -> .docx
                string rbReport = RobotContent.DefaultAnalysisMarkdown(rbDash, rbWords, avgAtt, avgMed, domKey);
                try
                {
                    Status?.Invoke("Writing the robot analysis report with LM Studio…");
                    using var c2 = new LmStudioClient(_config.LmStudioUrl);
                    string m2 = _config.Model;
                    if (string.IsNullOrWhiteSpace(m2)) m2 = await c2.GetFirstModelAsync(ct) ?? "";
                    if (!string.IsNullOrWhiteSpace(m2))
                    {
                        var p2 = RobotPromptBuilder.BuildAnalysis(rbSeed, seconds, avgAtt, avgMed, domKey, profile);
                        var r2 = RewritePromptBuilder.CleanReply(await c2.CompleteAsync(m2, p2.System, p2.User, ct));
                        if (!string.IsNullOrWhiteSpace(r2)) rbReport = r2;
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio offline — used built-in robot analysis report.");
                }
                DocxArticleWriter.Write(rbReport, Path.Combine(OutputDirectory, "robot_analysis_report.docx"), rbFont);

                // LM 3 — engineering report -> .docx
                string rbEng = RobotContent.DefaultEngineeringMarkdown(rbWords, avgAtt, avgMed, domKey);
                try
                {
                    Status?.Invoke("Writing the robot engineering report with LM Studio…");
                    using var c3 = new LmStudioClient(_config.LmStudioUrl);
                    string m3 = _config.Model;
                    if (string.IsNullOrWhiteSpace(m3)) m3 = await c3.GetFirstModelAsync(ct) ?? "";
                    if (!string.IsNullOrWhiteSpace(m3))
                    {
                        var p3 = RobotPromptBuilder.BuildEngineering(rbSeed, avgAtt, avgMed, domKey, profile);
                        var r3 = RewritePromptBuilder.CleanReply(await c3.CompleteAsync(m3, p3.System, p3.User, ct));
                        if (!string.IsNullOrWhiteSpace(r3)) rbEng = r3;
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio offline — used built-in robot engineering report.");
                }
                DocxArticleWriter.Write(rbEng, Path.Combine(OutputDirectory, "robot_engineering_report.docx"), rbFont);

                // LM 4 — 10-slide deck (accept only a full 10-slide LM deck)
                var rbDeck = RobotContent.DefaultDeck(rbDash, rbWords, avgAtt, avgMed, domKey);
                try
                {
                    Status?.Invoke("Building the 10-slide deck with LM Studio…");
                    using var c4 = new LmStudioClient(_config.LmStudioUrl);
                    string m4 = _config.Model;
                    if (string.IsNullOrWhiteSpace(m4)) m4 = await c4.GetFirstModelAsync(ct) ?? "";
                    if (!string.IsNullOrWhiteSpace(m4))
                    {
                        var p4 = RobotPromptBuilder.BuildSlides(avgAtt, avgMed, domKey);
                        var r4 = RewritePromptBuilder.CleanReply(await c4.CompleteAsync(m4, p4.System, p4.User, ct));
                        var parsed = PptxArticleWriter.ParseSlides(r4, 10);
                        if (parsed.Count == 10) rbDeck = parsed; // else keep the deterministic 10-slide deck
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio offline — used built-in 10-slide deck.");
                }
                PptxArticleWriter.Write(rbDeck, Path.Combine(OutputDirectory, "robot_system_analysis.pptx"), rbFont);

                Result?.Invoke(RobotContent.Dashboard(rbDash, rbPersonality, rbSkills) + "\n\n" + rbBrain);
                Status?.Invoke($"Saved your robot package to {OutputDirectory}.");
                Completed?.Invoke(Path.Combine(OutputDirectory, "robot_analysis_report.docx"));
                return;
            }

            // ===== Robotics: deterministic robot design + optional vision + 6 LM calls =====
            if (robotics)
            {
                Directory.CreateDirectory(OutputDirectory);
                var rxWords = accumulator.Words;
                string rxSeed = accumulator.Seed();
                int rxDistinct = rxWords.Distinct(StringComparer.OrdinalIgnoreCase).Count();
                var dataDir = Path.Combine(AppContext.BaseDirectory, "data");

                // recorded_eeg.csv + translated_eeg.txt
                File.WriteAllText(Path.Combine(OutputDirectory, "recorded_eeg.csv"), NlpContent.RecordedEegCsv(rxWords));
                string rxStats =
                    "# Translated EEG\n" +
                    $"recorded_seconds: {seconds}\n" +
                    $"words: {accumulator.Count}\n" +
                    $"distinct_words: {rxDistinct}\n" +
                    $"signal_quality: {(rxWords.Count > 0 ? "good" : "no signal")}\n" +
                    $"avg_attention: {avgAtt:0}\n" +
                    $"avg_meditation: {avgMed:0}\n" +
                    $"dominant_band: {domKey}\n\n";
                File.WriteAllText(Path.Combine(OutputDirectory, "translated_eeg.txt"), NlpContent.TranslatedText(rxStats, rxWords));

                // deterministic structured outputs
                var rxClasses = RoboticsClasses.DetectFromFile(dataDir, rxWords);
                var rxDash = RoboticsProfile.Dashboard(avgAtt, avgMed, bandReadings, rxWords);
                string rxTopClass = rxClasses.Count > 0 ? rxClasses[0].Class : "Service";

                File.WriteAllText(Path.Combine(OutputDirectory, "robot_classification.csv"), RoboticsContent.ClassScoresCsv(rxClasses));
                File.WriteAllText(Path.Combine(OutputDirectory, "robot_commands.csv"), RobotActions.DetectFromFile(dataDir).CommandsCsv(rxWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "robot_control_log.csv"), RoboticsContent.ControlLogCsv(rxWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "human_robot_interaction.csv"), RoboticsProfile.HumanInteractionCsv(rxDash));

                // history
                RoboticsProfile.AppendHistory(Path.Combine(OutputDirectory, "robotics_history.csv"), avgAtt, avgMed, bandReadings, rxWords, rxTopClass);

                // save this run as a timestamped recording so future runs can scan it
                var rxStamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                File.WriteAllText(Path.Combine(OutputDirectory, $"recorded_eeg_{rxStamp}.csv"), NlpContent.RecordedEegCsv(rxWords));

                string rxFont = string.IsNullOrWhiteSpace(_config.Font) ? "Verdana" : _config.Font;

                // robotic vision (optional robot_camera_images/ folder, LM vision, capped at 3)
                string rxVisionCsv = RoboticsContent.VisionFallbackCsv(rxWords);
                try
                {
                    var camDir = Path.Combine(OutputDirectory, "robot_camera_images");
                    if (Directory.Exists(camDir))
                    {
                        var imgs = Directory.EnumerateFiles(camDir)
                            .Where(f => new[] { ".png", ".jpg", ".jpeg", ".gif", ".webp" }.Contains(Path.GetExtension(f).ToLowerInvariant()))
                            .Take(3).ToList();
                        if (imgs.Count > 0)
                        {
                            Status?.Invoke($"Analyzing {imgs.Count} image(s) with LM Studio vision…");
                            using var ic = new LmStudioClient(_config.LmStudioUrl);
                            string im = _config.Model;
                            if (string.IsNullOrWhiteSpace(im)) im = await ic.GetFirstModelAsync(ct) ?? "";
                            var sb = new System.Text.StringBuilder("source,detection,category,confidence\n");
                            foreach (var img in imgs)
                            {
                                string detection = "object", category = "object";
                                if (!string.IsNullOrWhiteSpace(im))
                                {
                                    var ip = RoboticsPromptBuilder.BuildVision(rxSeed);
                                    var bytes = await File.ReadAllBytesAsync(img, ct);
                                    var visionReply = RewritePromptBuilder.CleanReply(
                                        await ic.CompleteWithImageAsync(im, ip.System, ip.User, bytes, RoboticsContent.MimeFor(img), ct));
                                    var firstLine = visionReply.Replace("\r", "").Split('\n').FirstOrDefault(l => l.Trim().Length > 0)?.Trim() ?? "";
                                    var pieces = firstLine.Split('-', 2);
                                    if (pieces.Length == 2) { detection = pieces[0].Replace(",", " ").Trim(); category = pieces[1].Replace(",", " ").Trim(); }
                                    else if (firstLine.Length > 0) { detection = firstLine.Replace(",", " ").Trim(); category = "object"; }
                                    if (detection.Length == 0) detection = "object";
                                    if (detection.Length > 60) detection = detection[..60];
                                    if (category.Length > 40) category = category[..40];
                                }
                                sb.AppendLine($"{Path.GetFileName(img)},{detection},{category},70");
                            }
                            rxVisionCsv = sb.ToString();
                        }
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio vision unavailable — used the deterministic vision list.");
                }
                File.WriteAllText(Path.Combine(OutputDirectory, "vision_analysis.csv"), rxVisionCsv);

                // LM 1 — concepts narratives (5 marked sections)
                string rxProfile = RoboticsContent.DefaultRobotProfile(rxWords);
                string rxArch = RoboticsContent.DefaultBrainArchitecture(rxWords);
                string rxAuto = RoboticsContent.DefaultAutonomousBehavior(rxWords);
                string rxElec = RoboticsContent.DefaultElectronicsDesign(rxWords);
                string rxBlender = RoboticsContent.DefaultBlenderPrompt(rxWords);
                try
                {
                    Status?.Invoke("Generating robot concepts with LM Studio…");
                    using var c1 = new LmStudioClient(_config.LmStudioUrl);
                    string m1 = _config.Model;
                    if (string.IsNullOrWhiteSpace(m1)) m1 = await c1.GetFirstModelAsync(ct) ?? "";
                    if (!string.IsNullOrWhiteSpace(m1))
                    {
                        var p1 = RoboticsPromptBuilder.BuildConcepts(rxSeed, avgAtt, avgMed, domKey, profile);
                        var r1 = RewritePromptBuilder.CleanReply(await c1.CompleteAsync(m1, p1.System, p1.User, ct));
                        var pr = NlpContent.ExtractTextSection(r1, "# ROBOT PROFILE", "# BRAIN ARCHITECTURE");
                        var ar = NlpContent.ExtractTextSection(r1, "# BRAIN ARCHITECTURE", "# AUTONOMOUS BEHAVIOR");
                        var au = NlpContent.ExtractTextSection(r1, "# AUTONOMOUS BEHAVIOR", "# ELECTRONICS");
                        var el = NlpContent.ExtractTextSection(r1, "# ELECTRONICS", "# BLENDER PROMPT");
                        var bl = NlpContent.ExtractTextSection(r1, "# BLENDER PROMPT", null);
                        if (!string.IsNullOrWhiteSpace(pr)) rxProfile = pr;
                        if (!string.IsNullOrWhiteSpace(ar)) rxArch = ar;
                        if (!string.IsNullOrWhiteSpace(au)) rxAuto = au;
                        if (!string.IsNullOrWhiteSpace(el)) rxElec = el;
                        if (!string.IsNullOrWhiteSpace(bl)) rxBlender = bl;
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio offline — used built-in robot concepts.");
                }
                File.WriteAllText(Path.Combine(OutputDirectory, "robot_profile.txt"), rxProfile);
                File.WriteAllText(Path.Combine(OutputDirectory, "robot_brain_architecture.md"), rxArch);
                File.WriteAllText(Path.Combine(OutputDirectory, "autonomous_behavior.txt"), rxAuto);
                File.WriteAllText(Path.Combine(OutputDirectory, "robot_electronics_design.md"), rxElec);
                File.WriteAllText(Path.Combine(OutputDirectory, "blender_robot_prompt.txt"), rxBlender);

                // LM 2 — development narratives (4 marked sections)
                string rxSim = RoboticsContent.DefaultSimulationScenarios(rxWords);
                string rxSwarm = RoboticsContent.DefaultSwarmDesign(rxWords);
                string rxLearn = RoboticsContent.DefaultLearningPlan(rxWords);
                string rxFuture = RoboticsContent.DefaultFutureRobotics(rxWords);
                try
                {
                    Status?.Invoke("Planning robot development with LM Studio…");
                    using var c2 = new LmStudioClient(_config.LmStudioUrl);
                    string m2 = _config.Model;
                    if (string.IsNullOrWhiteSpace(m2)) m2 = await c2.GetFirstModelAsync(ct) ?? "";
                    if (!string.IsNullOrWhiteSpace(m2))
                    {
                        var p2 = RoboticsPromptBuilder.BuildDevelopment(rxSeed, avgAtt, avgMed, domKey, profile);
                        var r2 = RewritePromptBuilder.CleanReply(await c2.CompleteAsync(m2, p2.System, p2.User, ct));
                        var si = NlpContent.ExtractTextSection(r2, "# SIMULATION SCENARIOS", "# SWARM DESIGN");
                        var sw = NlpContent.ExtractTextSection(r2, "# SWARM DESIGN", "# LEARNING PLAN");
                        var le = NlpContent.ExtractTextSection(r2, "# LEARNING PLAN", "# FUTURE ROBOTICS");
                        var fu = NlpContent.ExtractTextSection(r2, "# FUTURE ROBOTICS", null);
                        if (!string.IsNullOrWhiteSpace(si)) rxSim = si;
                        if (!string.IsNullOrWhiteSpace(sw)) rxSwarm = sw;
                        if (!string.IsNullOrWhiteSpace(le)) rxLearn = le;
                        if (!string.IsNullOrWhiteSpace(fu)) rxFuture = fu;
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio offline — used built-in development plans.");
                }
                File.WriteAllText(Path.Combine(OutputDirectory, "robot_simulation_scenarios.txt"), rxSim);
                File.WriteAllText(Path.Combine(OutputDirectory, "robot_swarm_design.md"), rxSwarm);
                File.WriteAllText(Path.Combine(OutputDirectory, "robot_learning_plan.txt"), rxLearn);
                File.WriteAllText(Path.Combine(OutputDirectory, "future_robotics_report.txt"), rxFuture);

                // LM 3 — design report -> .docx
                string rxDesign = RoboticsContent.DefaultDesignReportMarkdown(rxClasses, rxDash, rxWords);
                try
                {
                    Status?.Invoke("Writing the robot design report with LM Studio…");
                    using var c3 = new LmStudioClient(_config.LmStudioUrl);
                    string m3 = _config.Model;
                    if (string.IsNullOrWhiteSpace(m3)) m3 = await c3.GetFirstModelAsync(ct) ?? "";
                    if (!string.IsNullOrWhiteSpace(m3))
                    {
                        var p3 = RoboticsPromptBuilder.BuildDesignReport(rxSeed, rxClasses, avgAtt, avgMed, domKey, profile);
                        var r3 = RewritePromptBuilder.CleanReply(await c3.CompleteAsync(m3, p3.System, p3.User, ct));
                        if (!string.IsNullOrWhiteSpace(r3)) rxDesign = r3;
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio offline — used built-in design report.");
                }
                DocxArticleWriter.Write(rxDesign, Path.Combine(OutputDirectory, "robot_design_report.docx"), rxFont);

                // LM 4 — research paper -> .docx
                string rxPaper = RoboticsContent.DefaultResearchPaperMarkdown(rxClasses, rxWords, avgAtt, avgMed, domKey);
                try
                {
                    Status?.Invoke("Writing the robotics research paper with LM Studio…");
                    using var c4 = new LmStudioClient(_config.LmStudioUrl);
                    string m4 = _config.Model;
                    if (string.IsNullOrWhiteSpace(m4)) m4 = await c4.GetFirstModelAsync(ct) ?? "";
                    if (!string.IsNullOrWhiteSpace(m4))
                    {
                        var p4 = RoboticsPromptBuilder.BuildResearchPaper(rxSeed, seconds, rxClasses, avgAtt, avgMed, domKey, profile);
                        var r4 = RewritePromptBuilder.CleanReply(await c4.CompleteAsync(m4, p4.System, p4.User, ct));
                        if (!string.IsNullOrWhiteSpace(r4)) rxPaper = r4;
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio offline — used built-in research paper.");
                }
                DocxArticleWriter.Write(rxPaper, Path.Combine(OutputDirectory, "robotics_research_paper.docx"), rxFont);

                // LM 5 — engineering concepts -> .pdf
                string rxConcepts = RoboticsContent.DefaultConceptsMarkdown(rxWords);
                try
                {
                    Status?.Invoke("Writing the robotics concepts PDF with LM Studio…");
                    using var c5 = new LmStudioClient(_config.LmStudioUrl);
                    string m5 = _config.Model;
                    if (string.IsNullOrWhiteSpace(m5)) m5 = await c5.GetFirstModelAsync(ct) ?? "";
                    if (!string.IsNullOrWhiteSpace(m5))
                    {
                        var p5 = RoboticsPromptBuilder.BuildConceptsReport(rxSeed, avgAtt, avgMed, domKey, profile);
                        var r5 = RewritePromptBuilder.CleanReply(await c5.CompleteAsync(m5, p5.System, p5.User, ct));
                        if (!string.IsNullOrWhiteSpace(r5)) rxConcepts = r5;
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio offline — used built-in robotics concepts.");
                }
                PdfArticleWriter.Write(rxConcepts, Path.Combine(OutputDirectory, "robotics_concepts.pdf"), rxFont);

                // LM 6 — 10-slide deck (accept only a full 10-slide LM deck)
                var rxDeck = RoboticsContent.DefaultDeck(rxDash, rxWords, avgAtt, avgMed, domKey);
                try
                {
                    Status?.Invoke("Building the 10-slide deck with LM Studio…");
                    using var c6 = new LmStudioClient(_config.LmStudioUrl);
                    string m6 = _config.Model;
                    if (string.IsNullOrWhiteSpace(m6)) m6 = await c6.GetFirstModelAsync(ct) ?? "";
                    if (!string.IsNullOrWhiteSpace(m6))
                    {
                        var p6 = RoboticsPromptBuilder.BuildSlides(avgAtt, avgMed, domKey);
                        var r6 = RewritePromptBuilder.CleanReply(await c6.CompleteAsync(m6, p6.System, p6.User, ct));
                        var parsed = PptxArticleWriter.ParseSlides(r6, 10);
                        if (parsed.Count == 10) rxDeck = parsed; // else keep the deterministic 10-slide deck
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio offline — used built-in 10-slide deck.");
                }
                PptxArticleWriter.Write(rxDeck, Path.Combine(OutputDirectory, "robotics_analysis.pptx"), rxFont);

                Result?.Invoke(RoboticsContent.Dashboard(rxDash, rxClasses) + "\n\n" + rxProfile);
                Status?.Invoke($"Saved your robotics package to {OutputDirectory}.");
                Completed?.Invoke(Path.Combine(OutputDirectory, "robot_design_report.docx"));
                return;
            }

            // ===== Self-Awareness AI: deterministic reflection + history + 3 LM calls =====
            if (selfaware)
            {
                Directory.CreateDirectory(OutputDirectory);
                var saWords = accumulator.Words;
                string saSeed = accumulator.Seed();
                int saDistinct = saWords.Distinct(StringComparer.OrdinalIgnoreCase).Count();
                var dataDir = Path.Combine(AppContext.BaseDirectory, "data");

                // recorded_eeg.csv + translated_eeg.txt
                File.WriteAllText(Path.Combine(OutputDirectory, "recorded_eeg.csv"), NlpContent.RecordedEegCsv(saWords));
                string saStats =
                    "# Translated EEG\n" +
                    $"recorded_seconds: {seconds}\n" +
                    $"words: {accumulator.Count}\n" +
                    $"distinct_words: {saDistinct}\n" +
                    $"signal_quality: {(saWords.Count > 0 ? "good" : "no signal")}\n" +
                    $"avg_attention: {avgAtt:0}\n" +
                    $"avg_meditation: {avgMed:0}\n" +
                    $"dominant_band: {domKey}\n\n";
                File.WriteAllText(Path.Combine(OutputDirectory, "translated_eeg.txt"), NlpContent.TranslatedText(saStats, saWords));

                // deterministic reflection
                var saDomains = CuriosityDomains.DetectFromFile(dataDir, saWords);
                var saDash = SelfAwarenessProfile.Dashboard(avgAtt, avgMed, bandReadings, saWords);
                string saTopDomain = saDomains.Count > 0 ? saDomains[0].Domain : "Research";

                File.WriteAllText(Path.Combine(OutputDirectory, "recurring_thoughts.csv"), SelfAwarenessContent.RecurringThoughtsCsv(saWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "strength_profile.csv"), SelfAwarenessProfile.StrengthProfileCsv(avgAtt, avgMed, bandReadings, saWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "growth_opportunities.csv"), SelfAwarenessProfile.GrowthOpportunitiesCsv(avgAtt, avgMed, bandReadings, saWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "goal_analysis.csv"), SelfAwarenessProfile.GoalAnalysisCsv(avgAtt, avgMed, bandReadings, saWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "curiosity_map.csv"), SelfAwarenessContent.CuriosityMapCsv(saDomains));
                File.WriteAllText(Path.Combine(OutputDirectory, "self_questions.txt"), SelfAwarenessContent.SelfQuestionsText(saWords, saDomains));
                File.WriteAllText(Path.Combine(OutputDirectory, "personal_knowledge_graph.md"), SelfAwarenessContent.PersonalKnowledgeGraphMd(saWords, saDomains));
                File.WriteAllText(Path.Combine(OutputDirectory, "long_term_self_model.json"), SelfAwarenessContent.LongTermSelfModelJson(saWords, saDash, saDomains));

                // historical comparison (current + priors) + history
                File.WriteAllText(Path.Combine(OutputDirectory, "historical_comparison.csv"), SelfAwarenessScan.HistoricalComparisonCsv(OutputDirectory, saWords));
                SelfAwarenessProfile.AppendHistory(Path.Combine(OutputDirectory, "self_awareness_history.csv"), avgAtt, avgMed, bandReadings, saWords, saTopDomain);

                // save this run as a timestamped recording so future runs can scan it
                var saStamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                File.WriteAllText(Path.Combine(OutputDirectory, $"recorded_eeg_{saStamp}.csv"), NlpContent.RecordedEegCsv(saWords));

                string saFont = string.IsNullOrWhiteSpace(_config.Font) ? "Verdana" : _config.Font;

                // LM 1 — narratives (6 marked sections)
                string saProfile = SelfAwarenessContent.DefaultSelfProfile(saWords, saDomains);
                string saIdentity = SelfAwarenessContent.DefaultIdentity(saWords, saDomains);
                string saDialogue = SelfAwarenessContent.DefaultInternalDialogue(saWords);
                string saProjects = SelfAwarenessContent.DefaultProjects(saWords, saDomains);
                string saJournal = SelfAwarenessContent.DefaultReflectionJournal(saWords, saDomains);
                string saMentor = SelfAwarenessContent.DefaultMentor(saWords, saDomains);
                try
                {
                    Status?.Invoke("Reflecting with LM Studio…");
                    using var c1 = new LmStudioClient(_config.LmStudioUrl);
                    string m1 = _config.Model;
                    if (string.IsNullOrWhiteSpace(m1)) m1 = await c1.GetFirstModelAsync(ct) ?? "";
                    if (!string.IsNullOrWhiteSpace(m1))
                    {
                        var p1 = SelfAwarenessPromptBuilder.BuildNarratives(saSeed, saDomains, avgAtt, avgMed, domKey, profile);
                        var r1 = RewritePromptBuilder.CleanReply(await c1.CompleteAsync(m1, p1.System, p1.User, ct));
                        var sp = NlpContent.ExtractTextSection(r1, "# SELF PROFILE", "# IDENTITY");
                        var id = NlpContent.ExtractTextSection(r1, "# IDENTITY", "# INTERNAL DIALOGUE");
                        var dl = NlpContent.ExtractTextSection(r1, "# INTERNAL DIALOGUE", "# PROJECTS");
                        var pj = NlpContent.ExtractTextSection(r1, "# PROJECTS", "# REFLECTION JOURNAL");
                        var jr = NlpContent.ExtractTextSection(r1, "# REFLECTION JOURNAL", "# MENTOR");
                        var mt = NlpContent.ExtractTextSection(r1, "# MENTOR", null);
                        if (!string.IsNullOrWhiteSpace(sp)) saProfile = sp;
                        if (!string.IsNullOrWhiteSpace(id)) saIdentity = id;
                        if (!string.IsNullOrWhiteSpace(dl)) saDialogue = dl;
                        if (!string.IsNullOrWhiteSpace(pj)) saProjects = pj;
                        if (!string.IsNullOrWhiteSpace(jr)) saJournal = jr;
                        if (!string.IsNullOrWhiteSpace(mt)) saMentor = mt;
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio offline — used built-in reflection narratives.");
                }
                File.WriteAllText(Path.Combine(OutputDirectory, "self_profile.md"), saProfile);
                File.WriteAllText(Path.Combine(OutputDirectory, "identity_profile.md"), saIdentity);
                File.WriteAllText(Path.Combine(OutputDirectory, "internal_dialogue.txt"), saDialogue);
                File.WriteAllText(Path.Combine(OutputDirectory, "project_recommendations.md"), saProjects);
                File.WriteAllText(Path.Combine(OutputDirectory, "reflection_journal.txt"), saJournal);
                File.WriteAllText(Path.Combine(OutputDirectory, "mentor_history.txt"), saMentor);

                // LM 2 — development report -> .docx
                string saReport = SelfAwarenessContent.DefaultReportMarkdown(saDash, saDomains, saWords, avgAtt, avgMed, domKey);
                try
                {
                    Status?.Invoke("Writing the self-awareness report with LM Studio…");
                    using var c2 = new LmStudioClient(_config.LmStudioUrl);
                    string m2 = _config.Model;
                    if (string.IsNullOrWhiteSpace(m2)) m2 = await c2.GetFirstModelAsync(ct) ?? "";
                    if (!string.IsNullOrWhiteSpace(m2))
                    {
                        var p2 = SelfAwarenessPromptBuilder.BuildReport(saSeed, seconds, saDomains, avgAtt, avgMed, domKey, profile);
                        var r2 = RewritePromptBuilder.CleanReply(await c2.CompleteAsync(m2, p2.System, p2.User, ct));
                        if (!string.IsNullOrWhiteSpace(r2)) saReport = r2;
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio offline — used built-in self-awareness report.");
                }
                DocxArticleWriter.Write(saReport, Path.Combine(OutputDirectory, "self_awareness_report.docx"), saFont);

                // LM 3 — 10-slide deck (accept only a full 10-slide LM deck)
                var saDeck = SelfAwarenessContent.DefaultDeck(saDash, saDomains, saWords, avgAtt, avgMed, domKey);
                try
                {
                    Status?.Invoke("Building the 10-slide deck with LM Studio…");
                    using var c3 = new LmStudioClient(_config.LmStudioUrl);
                    string m3 = _config.Model;
                    if (string.IsNullOrWhiteSpace(m3)) m3 = await c3.GetFirstModelAsync(ct) ?? "";
                    if (!string.IsNullOrWhiteSpace(m3))
                    {
                        var p3 = SelfAwarenessPromptBuilder.BuildSlides(saDomains, avgAtt, avgMed, domKey);
                        var r3 = RewritePromptBuilder.CleanReply(await c3.CompleteAsync(m3, p3.System, p3.User, ct));
                        var parsed = PptxArticleWriter.ParseSlides(r3, 10);
                        if (parsed.Count == 10) saDeck = parsed; // else keep the deterministic 10-slide deck
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio offline — used built-in 10-slide deck.");
                }
                PptxArticleWriter.Write(saDeck, Path.Combine(OutputDirectory, "self_awareness_analysis.pptx"), saFont);

                Result?.Invoke(SelfAwarenessContent.Dashboard(saDash, saDomains) + "\n\n" + saProfile);
                Status?.Invoke($"Saved your self-awareness package to {OutputDirectory}.");
                Completed?.Invoke(Path.Combine(OutputDirectory, "self_awareness_report.docx"));
                return;
            }

            // ===== Semi-Supervised Learning: deterministic learning + evolution + network + 3 LM calls =====
            if (semisup)
            {
                Directory.CreateDirectory(OutputDirectory);
                var ssWords = accumulator.Words;
                string ssSeed = accumulator.Seed();
                int ssDistinct = ssWords.Distinct(StringComparer.OrdinalIgnoreCase).Count();
                var dataDir = Path.Combine(AppContext.BaseDirectory, "data");

                // recorded_eeg.csv + translated_eeg.txt
                File.WriteAllText(Path.Combine(OutputDirectory, "recorded_eeg.csv"), NlpContent.RecordedEegCsv(ssWords));
                string ssStats =
                    "# Translated EEG\n" +
                    $"recorded_seconds: {seconds}\n" +
                    $"words: {accumulator.Count}\n" +
                    $"distinct_words: {ssDistinct}\n" +
                    $"signal_quality: {(ssWords.Count > 0 ? "good" : "no signal")}\n" +
                    $"avg_attention: {avgAtt:0}\n" +
                    $"avg_meditation: {avgMed:0}\n" +
                    $"dominant_band: {domKey}\n\n";
                File.WriteAllText(Path.Combine(OutputDirectory, "translated_eeg.txt"), NlpContent.TranslatedText(ssStats, ssWords));

                // deterministic learning
                var ssCats = SemiSupCategories.DetectFromFile(dataDir, ssWords);
                var ssDash = SemiSupProfile.Dashboard(avgAtt, avgMed, bandReadings, ssWords);
                string ssTopCat = ssCats.Count > 0 ? ssCats[0].Category : "General";

                File.WriteAllText(Path.Combine(OutputDirectory, "labeled_knowledge.csv"), SemiSupContent.LabeledKnowledgeCsv(ssWords, ssCats));
                File.WriteAllText(Path.Combine(OutputDirectory, "unlabeled_discoveries.csv"), SemiSupContent.UnlabeledDiscoveriesCsv(ssWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "classification_results.csv"), SemiSupContent.ClassificationCsv(ssCats));
                File.WriteAllText(Path.Combine(OutputDirectory, "brain_clusters.csv"), SemiSupProfile.BrainClustersCsv(avgAtt, avgMed, bandReadings, ssWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "expanded_knowledge_base.csv"), SemiSupContent.ExpandedKnowledgeBaseCsv(ssWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "learning_progress.csv"), SemiSupProfile.LearningProgressCsv(avgAtt, avgMed, bandReadings, ssWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "confidence_scores.csv"), SemiSupProfile.ConfidenceScoresCsv(avgAtt, avgMed, bandReadings, ssWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "topic_predictions.csv"), SemiSupContent.TopicPredictionsCsv(ssCats));
                File.WriteAllText(Path.Combine(OutputDirectory, "brain_model.json"), SemiSupContent.BrainModelJson(ssWords, ssDash, ssCats));
                File.WriteAllText(Path.Combine(OutputDirectory, "semi_supervised_knowledge_graph.md"), SemiSupContent.KnowledgeGraphMd(ssWords, ssCats));

                // session evolution (current + priors) + network (csv_files) + history
                File.WriteAllText(Path.Combine(OutputDirectory, "session_evolution.csv"), SemiSupScan.SessionEvolutionCsv(OutputDirectory, ssWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "network_learning.csv"), SemiSupScan.NetworkLearningCsv(OutputDirectory));
                SemiSupProfile.AppendHistory(Path.Combine(OutputDirectory, "semi_supervised_history.csv"), avgAtt, avgMed, bandReadings, ssWords, ssTopCat);

                // save this run as a timestamped recording so future runs can scan it
                var ssStamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                File.WriteAllText(Path.Combine(OutputDirectory, $"recorded_eeg_{ssStamp}.csv"), NlpContent.RecordedEegCsv(ssWords));

                string ssFont = string.IsNullOrWhiteSpace(_config.Font) ? "Verdana" : _config.Font;

                // LM 1 — discoveries (2 marked sections)
                string ssConcept = SemiSupContent.DefaultConceptDiscovery(ssWords);
                string ssAi = SemiSupContent.DefaultAiDiscoveries(ssWords);
                try
                {
                    Status?.Invoke("Discovering concepts with LM Studio…");
                    using var c1 = new LmStudioClient(_config.LmStudioUrl);
                    string m1 = _config.Model;
                    if (string.IsNullOrWhiteSpace(m1)) m1 = await c1.GetFirstModelAsync(ct) ?? "";
                    if (!string.IsNullOrWhiteSpace(m1))
                    {
                        var p1 = SemiSupPromptBuilder.BuildDiscoveries(ssSeed, ssCats, avgAtt, avgMed, domKey, profile);
                        var r1 = RewritePromptBuilder.CleanReply(await c1.CompleteAsync(m1, p1.System, p1.User, ct));
                        var cd = NlpContent.ExtractTextSection(r1, "# CONCEPT DISCOVERY", "# AI DISCOVERIES");
                        var ad = NlpContent.ExtractTextSection(r1, "# AI DISCOVERIES", null);
                        if (!string.IsNullOrWhiteSpace(cd)) ssConcept = cd;
                        if (!string.IsNullOrWhiteSpace(ad)) ssAi = ad;
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio offline — used built-in discovery narratives.");
                }
                File.WriteAllText(Path.Combine(OutputDirectory, "concept_discovery.txt"), ssConcept);
                File.WriteAllText(Path.Combine(OutputDirectory, "ai_assisted_discoveries.txt"), ssAi);

                // LM 2 — report -> .docx
                string ssReport = SemiSupContent.DefaultReportMarkdown(ssDash, ssCats, ssWords, avgAtt, avgMed, domKey);
                try
                {
                    Status?.Invoke("Writing the semi-supervised learning report with LM Studio…");
                    using var c2 = new LmStudioClient(_config.LmStudioUrl);
                    string m2 = _config.Model;
                    if (string.IsNullOrWhiteSpace(m2)) m2 = await c2.GetFirstModelAsync(ct) ?? "";
                    if (!string.IsNullOrWhiteSpace(m2))
                    {
                        var p2 = SemiSupPromptBuilder.BuildReport(ssSeed, seconds, ssCats, avgAtt, avgMed, domKey, profile);
                        var r2 = RewritePromptBuilder.CleanReply(await c2.CompleteAsync(m2, p2.System, p2.User, ct));
                        if (!string.IsNullOrWhiteSpace(r2)) ssReport = r2;
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio offline — used built-in report.");
                }
                DocxArticleWriter.Write(ssReport, Path.Combine(OutputDirectory, "semi_supervised_learning_report.docx"), ssFont);

                // LM 3 — 10-slide deck (accept only a full 10-slide LM deck)
                var ssDeck = SemiSupContent.DefaultDeck(ssDash, ssCats, ssWords, avgAtt, avgMed, domKey);
                try
                {
                    Status?.Invoke("Building the 10-slide deck with LM Studio…");
                    using var c3 = new LmStudioClient(_config.LmStudioUrl);
                    string m3 = _config.Model;
                    if (string.IsNullOrWhiteSpace(m3)) m3 = await c3.GetFirstModelAsync(ct) ?? "";
                    if (!string.IsNullOrWhiteSpace(m3))
                    {
                        var p3 = SemiSupPromptBuilder.BuildSlides(ssCats, avgAtt, avgMed, domKey);
                        var r3 = RewritePromptBuilder.CleanReply(await c3.CompleteAsync(m3, p3.System, p3.User, ct));
                        var parsed = PptxArticleWriter.ParseSlides(r3, 10);
                        if (parsed.Count == 10) ssDeck = parsed; // else keep the deterministic 10-slide deck
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio offline — used built-in 10-slide deck.");
                }
                PptxArticleWriter.Write(ssDeck, Path.Combine(OutputDirectory, "semi_supervised_learning_analysis.pptx"), ssFont);

                Result?.Invoke(SemiSupContent.Dashboard(ssDash, ssCats) + "\n\n" + ssConcept);
                Status?.Invoke($"Saved your semi-supervised learning package to {OutputDirectory}.");
                Completed?.Invoke(Path.Combine(OutputDirectory, "semi_supervised_learning_report.docx"));
                return;
            }

            // ===== Sensorimotor Learning: deterministic sensorimotor model + 3 LM calls =====
            if (sensorimotor)
            {
                Directory.CreateDirectory(OutputDirectory);
                var smWords = accumulator.Words;
                string smSeed = accumulator.Seed();
                int smDistinct = smWords.Distinct(StringComparer.OrdinalIgnoreCase).Count();
                var dataDir = Path.Combine(AppContext.BaseDirectory, "data");

                // recorded_eeg.csv + translated_eeg.txt
                File.WriteAllText(Path.Combine(OutputDirectory, "recorded_eeg.csv"), NlpContent.RecordedEegCsv(smWords));
                string smStats =
                    "# Translated EEG\n" +
                    $"recorded_seconds: {seconds}\n" +
                    $"words: {accumulator.Count}\n" +
                    $"distinct_words: {smDistinct}\n" +
                    $"signal_quality: {(smWords.Count > 0 ? "good" : "no signal")}\n" +
                    $"avg_attention: {avgAtt:0}\n" +
                    $"avg_meditation: {avgMed:0}\n" +
                    $"dominant_band: {domKey}\n\n";
                File.WriteAllText(Path.Combine(OutputDirectory, "translated_eeg.txt"), NlpContent.TranslatedText(smStats, smWords));

                // deterministic score-sets + motor mapping
                var smMotor = MotorMap.DetectFromFile(dataDir);
                var smDash = SensorimotorProfile.Dashboard(avgAtt, avgMed, bandReadings, smWords);
                var smStates = SensorimotorProfile.SensorimotorStates(avgAtt, avgMed, bandReadings, smWords);
                var smSkills = SensorimotorProfile.SkillDevelopment(avgAtt, avgMed, bandReadings, smWords);
                string smTopSkill = smSkills.OrderByDescending(s => s.Value).First().Skill;

                File.WriteAllText(Path.Combine(OutputDirectory, "sensory_processing.csv"), SensorimotorProfile.SensoryProcessingCsv(avgAtt, avgMed, bandReadings, smWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "motor_planning.csv"), SensorimotorProfile.MotorPlanningCsv(avgAtt, avgMed, bandReadings, smWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "reaction_analysis.csv"), SensorimotorProfile.ReactionAnalysisCsv(avgAtt, avgMed, bandReadings, smWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "coordination_profile.csv"), SensorimotorProfile.CoordinationProfileCsv(avgAtt, avgMed, bandReadings, smWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "motor_learning_profile.csv"), SensorimotorProfile.MotorLearningProfileCsv(avgAtt, avgMed, bandReadings, smWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "skill_development.csv"), SensorimotorProfile.SkillDevelopmentCsv(avgAtt, avgMed, bandReadings, smWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "sensorimotor_states.csv"), SensorimotorProfile.SensorimotorStatesCsv(avgAtt, avgMed, bandReadings, smWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "motor_commands.csv"), smMotor.MotorCommandsCsv(smWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "driving_performance.csv"), SensorimotorContent.DrivingPerformanceCsv(smDash));
                File.WriteAllText(Path.Combine(OutputDirectory, "bmi_control_log.csv"), smMotor.BmiControlLogCsv(smWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "adaptation_analysis.csv"), SensorimotorProfile.AdaptationAnalysisCsv(avgAtt, avgMed, bandReadings, smWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "human_vs_ai_control.csv"), SensorimotorContent.HumanVsAiControlCsv(smDash));
                File.WriteAllText(Path.Combine(OutputDirectory, "sensorimotor_patterns.csv"), SensorimotorContent.SensorimotorPatternsCsv(smWords));

                // history
                SensorimotorProfile.AppendHistory(Path.Combine(OutputDirectory, "sensorimotor_history.csv"), avgAtt, avgMed, bandReadings, smWords, smTopSkill);

                // save this run as a timestamped recording so future runs can scan it
                var smStamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                File.WriteAllText(Path.Combine(OutputDirectory, $"recorded_eeg_{smStamp}.csv"), NlpContent.RecordedEegCsv(smWords));

                string smFont = string.IsNullOrWhiteSpace(_config.Font) ? "Verdana" : _config.Font;

                // LM 1 — training recommendations -> .txt
                string smTraining = SensorimotorContent.DefaultTrainingRecommendations(smWords);
                try
                {
                    Status?.Invoke("Generating training recommendations with LM Studio…");
                    using var c1 = new LmStudioClient(_config.LmStudioUrl);
                    string m1 = _config.Model;
                    if (string.IsNullOrWhiteSpace(m1)) m1 = await c1.GetFirstModelAsync(ct) ?? "";
                    if (!string.IsNullOrWhiteSpace(m1))
                    {
                        var p1 = SensorimotorPromptBuilder.BuildTraining(smSeed, smDash, avgAtt, avgMed, domKey, profile);
                        var r1 = RewritePromptBuilder.CleanReply(await c1.CompleteAsync(m1, p1.System, p1.User, ct));
                        if (!string.IsNullOrWhiteSpace(r1)) smTraining = r1;
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio offline — used built-in training recommendations.");
                }
                File.WriteAllText(Path.Combine(OutputDirectory, "training_recommendations.txt"), smTraining);

                // LM 2 — report -> .docx
                string smReport = SensorimotorContent.DefaultReportMarkdown(smDash, smWords, avgAtt, avgMed, domKey);
                try
                {
                    Status?.Invoke("Writing the sensorimotor report with LM Studio…");
                    using var c2 = new LmStudioClient(_config.LmStudioUrl);
                    string m2 = _config.Model;
                    if (string.IsNullOrWhiteSpace(m2)) m2 = await c2.GetFirstModelAsync(ct) ?? "";
                    if (!string.IsNullOrWhiteSpace(m2))
                    {
                        var p2 = SensorimotorPromptBuilder.BuildReport(smSeed, seconds, smDash, avgAtt, avgMed, domKey, profile);
                        var r2 = RewritePromptBuilder.CleanReply(await c2.CompleteAsync(m2, p2.System, p2.User, ct));
                        if (!string.IsNullOrWhiteSpace(r2)) smReport = r2;
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio offline — used built-in sensorimotor report.");
                }
                DocxArticleWriter.Write(smReport, Path.Combine(OutputDirectory, "sensorimotor_learning_report.docx"), smFont);

                // LM 3 — 10-slide deck (accept only a full 10-slide LM deck)
                var smDeck = SensorimotorContent.DefaultDeck(smDash, smWords, avgAtt, avgMed, domKey);
                try
                {
                    Status?.Invoke("Building the 10-slide deck with LM Studio…");
                    using var c3 = new LmStudioClient(_config.LmStudioUrl);
                    string m3 = _config.Model;
                    if (string.IsNullOrWhiteSpace(m3)) m3 = await c3.GetFirstModelAsync(ct) ?? "";
                    if (!string.IsNullOrWhiteSpace(m3))
                    {
                        var p3 = SensorimotorPromptBuilder.BuildSlides(smDash, avgAtt, avgMed, domKey);
                        var r3 = RewritePromptBuilder.CleanReply(await c3.CompleteAsync(m3, p3.System, p3.User, ct));
                        var parsed = PptxArticleWriter.ParseSlides(r3, 10);
                        if (parsed.Count == 10) smDeck = parsed; // else keep the deterministic 10-slide deck
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio offline — used built-in 10-slide deck.");
                }
                PptxArticleWriter.Write(smDeck, Path.Combine(OutputDirectory, "sensorimotor_learning.pptx"), smFont);

                Result?.Invoke(SensorimotorContent.Dashboard(smDash, smStates) + "\n\n" + smTraining);
                Status?.Invoke($"Saved your sensorimotor learning package to {OutputDirectory}.");
                Completed?.Invoke(Path.Combine(OutputDirectory, "sensorimotor_learning_report.docx"));
                return;
            }

            // ===== Smart House: deterministic home intelligence + 4 LM calls =====
            if (smarthouse)
            {
                Directory.CreateDirectory(OutputDirectory);
                var shWords = accumulator.Words;
                string shSeed = accumulator.Seed();
                int shDistinct = shWords.Distinct(StringComparer.OrdinalIgnoreCase).Count();
                var dataDir = Path.Combine(AppContext.BaseDirectory, "data");

                // recorded_eeg.csv + translated_eeg.txt
                File.WriteAllText(Path.Combine(OutputDirectory, "recorded_eeg.csv"), NlpContent.RecordedEegCsv(shWords));
                string shStats =
                    "# Translated EEG\n" +
                    $"recorded_seconds: {seconds}\n" +
                    $"words: {accumulator.Count}\n" +
                    $"distinct_words: {shDistinct}\n" +
                    $"signal_quality: {(shWords.Count > 0 ? "good" : "no signal")}\n" +
                    $"avg_attention: {avgAtt:0}\n" +
                    $"avg_meditation: {avgMed:0}\n" +
                    $"dominant_band: {domKey}\n\n";
                File.WriteAllText(Path.Combine(OutputDirectory, "translated_eeg.txt"), NlpContent.TranslatedText(shStats, shWords));

                // deterministic home intelligence
                var shRooms = HouseRooms.DetectFromFile(dataDir, shWords);
                var shDash = SmartHouseProfile.Dashboard(avgAtt, avgMed, bandReadings, shWords);
                var shMoods = SmartHouseProfile.MoodScores(avgAtt, avgMed, bandReadings, shWords);
                string shTopRoom = shRooms.Count > 0 ? shRooms[0].Room : "Living Room";

                File.WriteAllText(Path.Combine(OutputDirectory, "smart_home_profile.csv"), SmartHouseProfile.SmartHomeProfileCsv(avgAtt, avgMed, bandReadings, shWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "room_preferences.csv"), SmartHouseContent.RoomPreferencesCsv(shRooms, shDash));
                File.WriteAllText(Path.Combine(OutputDirectory, "environment_preferences.csv"), SmartHouseProfile.EnvironmentPreferencesCsv(avgAtt, avgMed, bandReadings, shWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "device_recommendations.csv"), SmartHouseContent.DeviceRecommendationsCsv(shDash, shMoods));
                File.WriteAllText(Path.Combine(OutputDirectory, "daily_routines.csv"), SmartHouseProfile.DailyRoutinesCsv(avgAtt, avgMed, bandReadings, shWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "occupancy_predictions.csv"), SmartHouseContent.OccupancyPredictionsCsv(shRooms));
                File.WriteAllText(Path.Combine(OutputDirectory, "mood_automation.csv"), SmartHouseContent.MoodAutomationCsv(shMoods));
                File.WriteAllText(Path.Combine(OutputDirectory, "iot_device_registry.csv"), SmartHouseContent.IotDeviceRegistryCsv());

                // history
                SmartHouseProfile.AppendHistory(Path.Combine(OutputDirectory, "smart_house_history.csv"), avgAtt, avgMed, bandReadings, shWords, shTopRoom);

                // save this run as a timestamped recording so future runs can scan it
                var shStamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                File.WriteAllText(Path.Combine(OutputDirectory, $"recorded_eeg_{shStamp}.csv"), NlpContent.RecordedEegCsv(shWords));

                string shFont = string.IsNullOrWhiteSpace(_config.Font) ? "Verdana" : _config.Font;

                // LM 1 — narratives (5 marked sections)
                string shEnergy = SmartHouseContent.DefaultEnergyReport(shDash);
                string shSecurity = SmartHouseContent.DefaultSecurity(shDash);
                string shAssistant = SmartHouseContent.DefaultAssistantLog(shWords);
                string shDesign = SmartHouseContent.DefaultHomeDesign(shRooms);
                string shSim = SmartHouseContent.DefaultSimulation(shDash);
                try
                {
                    Status?.Invoke("Analyzing your smart home with LM Studio…");
                    using var c1 = new LmStudioClient(_config.LmStudioUrl);
                    string m1 = _config.Model;
                    if (string.IsNullOrWhiteSpace(m1)) m1 = await c1.GetFirstModelAsync(ct) ?? "";
                    if (!string.IsNullOrWhiteSpace(m1))
                    {
                        var p1 = SmartHousePromptBuilder.BuildNarratives(shSeed, shRooms, avgAtt, avgMed, domKey, profile);
                        var r1 = RewritePromptBuilder.CleanReply(await c1.CompleteAsync(m1, p1.System, p1.User, ct));
                        var en = NlpContent.ExtractTextSection(r1, "# ENERGY", "# SECURITY");
                        var se = NlpContent.ExtractTextSection(r1, "# SECURITY", "# ASSISTANT");
                        var asst = NlpContent.ExtractTextSection(r1, "# ASSISTANT", "# HOME DESIGN");
                        var de = NlpContent.ExtractTextSection(r1, "# HOME DESIGN", "# SIMULATION");
                        var si = NlpContent.ExtractTextSection(r1, "# SIMULATION", null);
                        if (!string.IsNullOrWhiteSpace(en)) shEnergy = en;
                        if (!string.IsNullOrWhiteSpace(se)) shSecurity = se;
                        if (!string.IsNullOrWhiteSpace(asst)) shAssistant = asst;
                        if (!string.IsNullOrWhiteSpace(de)) shDesign = de;
                        if (!string.IsNullOrWhiteSpace(si)) shSim = si;
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio offline — used built-in smart-home narratives.");
                }
                File.WriteAllText(Path.Combine(OutputDirectory, "energy_optimization_report.txt"), shEnergy);
                File.WriteAllText(Path.Combine(OutputDirectory, "security_recommendations.txt"), shSecurity);
                File.WriteAllText(Path.Combine(OutputDirectory, "house_assistant_log.txt"), shAssistant);
                File.WriteAllText(Path.Combine(OutputDirectory, "home_design_recommendations.md"), shDesign);
                File.WriteAllText(Path.Combine(OutputDirectory, "house_simulation_report.txt"), shSim);

                // LM 2 — future plan -> .docx
                string shFuture = SmartHouseContent.DefaultFuturePlanMarkdown(shRooms, shWords);
                try
                {
                    Status?.Invoke("Writing the future smart-home plan with LM Studio…");
                    using var c2 = new LmStudioClient(_config.LmStudioUrl);
                    string m2 = _config.Model;
                    if (string.IsNullOrWhiteSpace(m2)) m2 = await c2.GetFirstModelAsync(ct) ?? "";
                    if (!string.IsNullOrWhiteSpace(m2))
                    {
                        var p2 = SmartHousePromptBuilder.BuildFuturePlan(shSeed, shRooms, avgAtt, avgMed, domKey, profile);
                        var r2 = RewritePromptBuilder.CleanReply(await c2.CompleteAsync(m2, p2.System, p2.User, ct));
                        if (!string.IsNullOrWhiteSpace(r2)) shFuture = r2;
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio offline — used built-in future plan.");
                }
                DocxArticleWriter.Write(shFuture, Path.Combine(OutputDirectory, "future_smart_home_plan.docx"), shFont);

                // LM 3 — analysis report -> .docx
                string shReport = SmartHouseContent.DefaultAnalysisMarkdown(shDash, shRooms, shWords, avgAtt, avgMed, domKey);
                try
                {
                    Status?.Invoke("Writing the smart-house analysis report with LM Studio…");
                    using var c3 = new LmStudioClient(_config.LmStudioUrl);
                    string m3 = _config.Model;
                    if (string.IsNullOrWhiteSpace(m3)) m3 = await c3.GetFirstModelAsync(ct) ?? "";
                    if (!string.IsNullOrWhiteSpace(m3))
                    {
                        var p3 = SmartHousePromptBuilder.BuildAnalysis(shSeed, seconds, shRooms, avgAtt, avgMed, domKey, profile);
                        var r3 = RewritePromptBuilder.CleanReply(await c3.CompleteAsync(m3, p3.System, p3.User, ct));
                        if (!string.IsNullOrWhiteSpace(r3)) shReport = r3;
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio offline — used built-in analysis report.");
                }
                DocxArticleWriter.Write(shReport, Path.Combine(OutputDirectory, "smart_house_analysis_report.docx"), shFont);

                // LM 4 — 10-slide deck (accept only a full 10-slide LM deck)
                var shDeck = SmartHouseContent.DefaultDeck(shDash, shRooms, shWords, avgAtt, avgMed, domKey);
                try
                {
                    Status?.Invoke("Building the 10-slide deck with LM Studio…");
                    using var c4 = new LmStudioClient(_config.LmStudioUrl);
                    string m4 = _config.Model;
                    if (string.IsNullOrWhiteSpace(m4)) m4 = await c4.GetFirstModelAsync(ct) ?? "";
                    if (!string.IsNullOrWhiteSpace(m4))
                    {
                        var p4 = SmartHousePromptBuilder.BuildSlides(shRooms, avgAtt, avgMed, domKey);
                        var r4 = RewritePromptBuilder.CleanReply(await c4.CompleteAsync(m4, p4.System, p4.User, ct));
                        var parsed = PptxArticleWriter.ParseSlides(r4, 10);
                        if (parsed.Count == 10) shDeck = parsed; // else keep the deterministic 10-slide deck
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio offline — used built-in 10-slide deck.");
                }
                PptxArticleWriter.Write(shDeck, Path.Combine(OutputDirectory, "smart_house_presentation.pptx"), shFont);

                Result?.Invoke(SmartHouseContent.Dashboard(shDash, shRooms) + "\n\n" + shEnergy);
                Status?.Invoke($"Saved your smart-house package to {OutputDirectory}.");
                Completed?.Invoke(Path.Combine(OutputDirectory, "smart_house_analysis_report.docx"));
                return;
            }

            // ===== Strong AI: deterministic cognitive framework + 6 LM calls =====
            if (strongai)
            {
                Directory.CreateDirectory(OutputDirectory);
                var sa2Words = accumulator.Words;
                string sa2Seed = accumulator.Seed();
                int sa2Distinct = sa2Words.Distinct(StringComparer.OrdinalIgnoreCase).Count();
                var dataDir = Path.Combine(AppContext.BaseDirectory, "data");

                // recorded_eeg.csv + translated_eeg.txt
                File.WriteAllText(Path.Combine(OutputDirectory, "recorded_eeg.csv"), NlpContent.RecordedEegCsv(sa2Words));
                string sa2Stats =
                    "# Translated EEG\n" +
                    $"recorded_seconds: {seconds}\n" +
                    $"words: {accumulator.Count}\n" +
                    $"distinct_words: {sa2Distinct}\n" +
                    $"signal_quality: {(sa2Words.Count > 0 ? "good" : "no signal")}\n" +
                    $"avg_attention: {avgAtt:0}\n" +
                    $"avg_meditation: {avgMed:0}\n" +
                    $"dominant_band: {domKey}\n\n";
                File.WriteAllText(Path.Combine(OutputDirectory, "translated_eeg.txt"), NlpContent.TranslatedText(sa2Stats, sa2Words));

                // deterministic cognitive framework
                var sa2Domains = StrongAiDomains.DetectFromFile(dataDir, sa2Words);
                var sa2Dash = StrongAiProfile.Dashboard(avgAtt, avgMed, bandReadings, sa2Words);
                string sa2TopDomain = sa2Domains.Count > 0 ? sa2Domains[0].Domain : "General";

                File.WriteAllText(Path.Combine(OutputDirectory, "cognitive_knowledge_base.db"), StrongAiContent.KnowledgeBaseDb(sa2Words, sa2Domains));
                File.WriteAllText(Path.Combine(OutputDirectory, "goal_hierarchy.csv"), StrongAiContent.GoalHierarchyCsv(sa2Domains, sa2Dash));
                File.WriteAllText(Path.Combine(OutputDirectory, "cognitive_simulation.csv"), StrongAiProfile.CognitiveSimulationCsv(avgAtt, avgMed, bandReadings, sa2Words));
                File.WriteAllText(Path.Combine(OutputDirectory, "learning_progress.csv"), StrongAiProfile.LearningProgressCsv(avgAtt, avgMed, bandReadings, sa2Words));
                File.WriteAllText(Path.Combine(OutputDirectory, "strong_ai_knowledge_graph.md"), StrongAiContent.KnowledgeGraphMd(sa2Words, sa2Domains));

                // long-term memory
                StrongAiProfile.AppendMemory(Path.Combine(OutputDirectory, "strong_ai_memory.csv"), avgAtt, avgMed, bandReadings, sa2Words, sa2TopDomain);

                // save this run as a timestamped recording so future runs can scan it
                var sa2Stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                File.WriteAllText(Path.Combine(OutputDirectory, $"recorded_eeg_{sa2Stamp}.csv"), NlpContent.RecordedEegCsv(sa2Words));

                string sa2Font = string.IsNullOrWhiteSpace(_config.Font) ? "Verdana" : _config.Font;

                // LM 1 — cognition narratives (4 marked sections)
                string sa2Reasoning = StrongAiContent.DefaultReasoning(sa2Words);
                string sa2Reflect = StrongAiContent.DefaultSelfReflection(sa2Dash);
                string sa2Creativity = StrongAiContent.DefaultCreativity(sa2Words);
                string sa2Decision = StrongAiContent.DefaultDecision(sa2Dash);
                try
                {
                    Status?.Invoke("Reasoning with LM Studio…");
                    using var c1 = new LmStudioClient(_config.LmStudioUrl);
                    string m1 = _config.Model;
                    if (string.IsNullOrWhiteSpace(m1)) m1 = await c1.GetFirstModelAsync(ct) ?? "";
                    if (!string.IsNullOrWhiteSpace(m1))
                    {
                        var p1 = StrongAiPromptBuilder.BuildCognition(sa2Seed, sa2Domains, avgAtt, avgMed, domKey, profile);
                        var r1 = RewritePromptBuilder.CleanReply(await c1.CompleteAsync(m1, p1.System, p1.User, ct));
                        var re = NlpContent.ExtractTextSection(r1, "# REASONING", "# SELF REFLECTION");
                        var sr = NlpContent.ExtractTextSection(r1, "# SELF REFLECTION", "# CREATIVITY");
                        var cr = NlpContent.ExtractTextSection(r1, "# CREATIVITY", "# DECISION");
                        var de = NlpContent.ExtractTextSection(r1, "# DECISION", null);
                        if (!string.IsNullOrWhiteSpace(re)) sa2Reasoning = re;
                        if (!string.IsNullOrWhiteSpace(sr)) sa2Reflect = sr;
                        if (!string.IsNullOrWhiteSpace(cr)) sa2Creativity = cr;
                        if (!string.IsNullOrWhiteSpace(de)) sa2Decision = de;
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio offline — used built-in cognition narratives.");
                }
                File.WriteAllText(Path.Combine(OutputDirectory, "reasoning_results.txt"), sa2Reasoning);
                File.WriteAllText(Path.Combine(OutputDirectory, "self_reflection_report.txt"), sa2Reflect);
                File.WriteAllText(Path.Combine(OutputDirectory, "creativity_report.txt"), sa2Creativity);
                File.WriteAllText(Path.Combine(OutputDirectory, "decision_analysis.txt"), sa2Decision);

                // LM 2 — world narratives (3 marked sections)
                string sa2Agents = StrongAiContent.DefaultAgentReports(sa2Domains, sa2Words);
                string sa2World = StrongAiContent.DefaultWorldModel(sa2Domains);
                string sa2Future = StrongAiContent.DefaultFuturePredictions(sa2Domains);
                try
                {
                    Status?.Invoke("Building the world model with LM Studio…");
                    using var c2 = new LmStudioClient(_config.LmStudioUrl);
                    string m2 = _config.Model;
                    if (string.IsNullOrWhiteSpace(m2)) m2 = await c2.GetFirstModelAsync(ct) ?? "";
                    if (!string.IsNullOrWhiteSpace(m2))
                    {
                        var p2 = StrongAiPromptBuilder.BuildWorld(sa2Seed, sa2Domains, avgAtt, avgMed, domKey, profile);
                        var r2 = RewritePromptBuilder.CleanReply(await c2.CompleteAsync(m2, p2.System, p2.User, ct));
                        var ag = NlpContent.ExtractTextSection(r2, "# AGENT REPORTS", "# WORLD MODEL");
                        var wm = NlpContent.ExtractTextSection(r2, "# WORLD MODEL", "# FUTURE PREDICTIONS");
                        var fp = NlpContent.ExtractTextSection(r2, "# FUTURE PREDICTIONS", null);
                        if (!string.IsNullOrWhiteSpace(ag)) sa2Agents = ag;
                        if (!string.IsNullOrWhiteSpace(wm)) sa2World = wm;
                        if (!string.IsNullOrWhiteSpace(fp)) sa2Future = fp;
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio offline — used built-in world model.");
                }
                File.WriteAllText(Path.Combine(OutputDirectory, "agent_reports.md"), sa2Agents);
                File.WriteAllText(Path.Combine(OutputDirectory, "world_model.md"), sa2World);
                File.WriteAllText(Path.Combine(OutputDirectory, "future_predictions.txt"), sa2Future);

                // LM 3 — problem solving -> .docx
                string sa2Problem = StrongAiContent.DefaultProblemSolvingMarkdown(sa2Domains, sa2Words);
                try
                {
                    Status?.Invoke("Solving problems with LM Studio…");
                    using var c3 = new LmStudioClient(_config.LmStudioUrl);
                    string m3 = _config.Model;
                    if (string.IsNullOrWhiteSpace(m3)) m3 = await c3.GetFirstModelAsync(ct) ?? "";
                    if (!string.IsNullOrWhiteSpace(m3))
                    {
                        var p3 = StrongAiPromptBuilder.BuildProblemSolving(sa2Seed, sa2Domains, avgAtt, avgMed, domKey, profile);
                        var r3 = RewritePromptBuilder.CleanReply(await c3.CompleteAsync(m3, p3.System, p3.User, ct));
                        if (!string.IsNullOrWhiteSpace(r3)) sa2Problem = r3;
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio offline — used built-in problem-solving report.");
                }
                DocxArticleWriter.Write(sa2Problem, Path.Combine(OutputDirectory, "problem_solving_report.docx"), sa2Font);

                // LM 4 — research opportunities -> .docx
                string sa2Research = StrongAiContent.DefaultResearchOpportunitiesMarkdown(sa2Domains, sa2Words);
                try
                {
                    Status?.Invoke("Finding research opportunities with LM Studio…");
                    using var c4 = new LmStudioClient(_config.LmStudioUrl);
                    string m4 = _config.Model;
                    if (string.IsNullOrWhiteSpace(m4)) m4 = await c4.GetFirstModelAsync(ct) ?? "";
                    if (!string.IsNullOrWhiteSpace(m4))
                    {
                        var p4 = StrongAiPromptBuilder.BuildResearch(sa2Seed, sa2Domains, avgAtt, avgMed, domKey, profile);
                        var r4 = RewritePromptBuilder.CleanReply(await c4.CompleteAsync(m4, p4.System, p4.User, ct));
                        if (!string.IsNullOrWhiteSpace(r4)) sa2Research = r4;
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio offline — used built-in research opportunities.");
                }
                DocxArticleWriter.Write(sa2Research, Path.Combine(OutputDirectory, "research_opportunities.docx"), sa2Font);

                // LM 5 — analysis report -> .docx
                string sa2Report = StrongAiContent.DefaultAnalysisMarkdown(sa2Dash, sa2Domains, sa2Words, avgAtt, avgMed, domKey);
                try
                {
                    Status?.Invoke("Writing the Strong AI analysis report with LM Studio…");
                    using var c5 = new LmStudioClient(_config.LmStudioUrl);
                    string m5 = _config.Model;
                    if (string.IsNullOrWhiteSpace(m5)) m5 = await c5.GetFirstModelAsync(ct) ?? "";
                    if (!string.IsNullOrWhiteSpace(m5))
                    {
                        var p5 = StrongAiPromptBuilder.BuildAnalysis(sa2Seed, seconds, sa2Domains, avgAtt, avgMed, domKey, profile);
                        var r5 = RewritePromptBuilder.CleanReply(await c5.CompleteAsync(m5, p5.System, p5.User, ct));
                        if (!string.IsNullOrWhiteSpace(r5)) sa2Report = r5;
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio offline — used built-in analysis report.");
                }
                DocxArticleWriter.Write(sa2Report, Path.Combine(OutputDirectory, "strong_ai_analysis_report.docx"), sa2Font);

                // LM 6 — 10-slide deck (accept only a full 10-slide LM deck)
                var sa2Deck = StrongAiContent.DefaultDeck(sa2Dash, sa2Domains, sa2Words, avgAtt, avgMed, domKey);
                try
                {
                    Status?.Invoke("Building the 10-slide deck with LM Studio…");
                    using var c6 = new LmStudioClient(_config.LmStudioUrl);
                    string m6 = _config.Model;
                    if (string.IsNullOrWhiteSpace(m6)) m6 = await c6.GetFirstModelAsync(ct) ?? "";
                    if (!string.IsNullOrWhiteSpace(m6))
                    {
                        var p6 = StrongAiPromptBuilder.BuildSlides(sa2Domains, avgAtt, avgMed, domKey);
                        var r6 = RewritePromptBuilder.CleanReply(await c6.CompleteAsync(m6, p6.System, p6.User, ct));
                        var parsed = PptxArticleWriter.ParseSlides(r6, 10);
                        if (parsed.Count == 10) sa2Deck = parsed; // else keep the deterministic 10-slide deck
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio offline — used built-in 10-slide deck.");
                }
                PptxArticleWriter.Write(sa2Deck, Path.Combine(OutputDirectory, "strong_ai_presentation.pptx"), sa2Font);

                Result?.Invoke(StrongAiContent.Dashboard(sa2Dash, sa2Domains) + "\n\n" + sa2Reasoning);
                Status?.Invoke($"Saved your Strong AI package to {OutputDirectory}.");
                Completed?.Invoke(Path.Combine(OutputDirectory, "strong_ai_analysis_report.docx"));
                return;
            }

            if (superi)
            {
                Directory.CreateDirectory(OutputDirectory);
                var siWords = accumulator.Words;
                string siSeed = accumulator.Seed();
                int siDistinct = siWords.Distinct(StringComparer.OrdinalIgnoreCase).Count();
                var dataDir = Path.Combine(AppContext.BaseDirectory, "data");

                // recorded_eeg.csv (+ timestamped copy so future runs can load/compare) + translated_eeg.txt
                File.WriteAllText(Path.Combine(OutputDirectory, "recorded_eeg.csv"), NlpContent.RecordedEegCsv(siWords));
                var siStamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                File.WriteAllText(Path.Combine(OutputDirectory, $"recorded_eeg_{siStamp}.csv"), NlpContent.RecordedEegCsv(siWords));
                string siStats =
                    "# Translated EEG\n" +
                    $"recorded_seconds: {seconds}\n" +
                    $"words: {accumulator.Count}\n" +
                    $"distinct_words: {siDistinct}\n" +
                    $"signal_quality: {(siWords.Count > 0 ? "good" : "no signal")}\n" +
                    $"avg_attention: {avgAtt:0}\n" +
                    $"avg_meditation: {avgMed:0}\n" +
                    $"dominant_band: {domKey}\n\n";
                File.WriteAllText(Path.Combine(OutputDirectory, "translated_eeg.txt"), NlpContent.TranslatedText(siStats, siWords));

                // deterministic cognitive framework (offline-safe)
                var siDomains = SuperintelligenceDomains.DetectFromFile(dataDir, siWords);
                var siDash = SuperintelligenceProfile.Dashboard(avgAtt, avgMed, bandReadings, siWords);
                string siTopDomain = siDomains.Count > 0 ? siDomains[0].Domain : "General";

                File.WriteAllText(Path.Combine(OutputDirectory, "cognitive_capabilities.csv"), SuperintelligenceProfile.CapabilitiesCsv(avgAtt, avgMed, bandReadings, siWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "knowledge_integration.csv"), SuperintelligenceProfile.KnowledgeIntegrationCsv(avgAtt, avgMed, bandReadings, siWords, siDomains));
                File.WriteAllText(Path.Combine(OutputDirectory, "innovation_assessment.csv"), SuperintelligenceProfile.InnovationCsv(avgAtt, avgMed, bandReadings, siWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "domain_intelligence_profile.csv"), SuperintelligenceProfile.DomainProfileCsv(siDomains));
                File.WriteAllText(Path.Combine(OutputDirectory, "expert_profile_comparison.csv"), SuperintelligenceProfile.ExpertComparisonCsv(siWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "grand_challenges.txt"), SuperintelligenceContent.GrandChallenges(siWords, siDomains));
                File.WriteAllText(Path.Combine(OutputDirectory, "superintelligence_knowledge_graph.md"), SuperintelligenceContent.KnowledgeGraphMd(siWords, siDomains));

                // historical development tracking
                SuperintelligenceProfile.AppendHistory(Path.Combine(OutputDirectory, "superintelligence_history.csv"), avgAtt, avgMed, bandReadings, siWords, siTopDomain);

                string siFont = string.IsNullOrWhiteSpace(_config.Font) ? "Verdana" : _config.Font;

                // LM 1 — four narrative sections (problem solving, systems thinking, future knowledge, growth)
                string siProblem = SuperintelligenceContent.DefaultProblemSolving(siDomains, siWords);
                string siSystems = SuperintelligenceContent.DefaultSystemsThinking(siDomains, siWords);
                string siFuture = SuperintelligenceContent.DefaultFutureKnowledge(siDomains);
                string siGrowth = SuperintelligenceContent.DefaultGrowthRecommendations(siDomains);
                try
                {
                    Status?.Invoke("Analyzing cognition with LM Studio…");
                    using var c1 = new LmStudioClient(_config.LmStudioUrl);
                    string m1 = _config.Model;
                    if (string.IsNullOrWhiteSpace(m1)) m1 = await c1.GetFirstModelAsync(ct) ?? "";
                    if (!string.IsNullOrWhiteSpace(m1))
                    {
                        var p1 = SuperintelligencePromptBuilder.BuildNarratives(siSeed, siDomains, avgAtt, avgMed, domKey, profile);
                        var r1 = RewritePromptBuilder.CleanReply(await c1.CompleteAsync(m1, p1.System, p1.User, ct));
                        var ps = NlpContent.ExtractTextSection(r1, "# PROBLEM SOLVING", "# SYSTEMS THINKING");
                        var st = NlpContent.ExtractTextSection(r1, "# SYSTEMS THINKING", "# FUTURE KNOWLEDGE");
                        var fk = NlpContent.ExtractTextSection(r1, "# FUTURE KNOWLEDGE", "# GROWTH");
                        var gr = NlpContent.ExtractTextSection(r1, "# GROWTH", null);
                        if (!string.IsNullOrWhiteSpace(ps)) siProblem = ps;
                        if (!string.IsNullOrWhiteSpace(st)) siSystems = st;
                        if (!string.IsNullOrWhiteSpace(fk)) siFuture = fk;
                        if (!string.IsNullOrWhiteSpace(gr)) siGrowth = gr;
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio offline — used built-in cognitive narratives.");
                }
                File.WriteAllText(Path.Combine(OutputDirectory, "problem_solving_report.txt"), siProblem);
                File.WriteAllText(Path.Combine(OutputDirectory, "systems_thinking_report.txt"), siSystems);
                File.WriteAllText(Path.Combine(OutputDirectory, "future_knowledge_simulation.txt"), siFuture);
                File.WriteAllText(Path.Combine(OutputDirectory, "growth_recommendations.txt"), siGrowth);

                // LM 2 — artificial research council -> .md
                string siCouncil = SuperintelligenceContent.DefaultResearchCouncil(siDomains, siWords);
                try
                {
                    Status?.Invoke("Convening the research council with LM Studio…");
                    using var c2 = new LmStudioClient(_config.LmStudioUrl);
                    string m2 = _config.Model;
                    if (string.IsNullOrWhiteSpace(m2)) m2 = await c2.GetFirstModelAsync(ct) ?? "";
                    if (!string.IsNullOrWhiteSpace(m2))
                    {
                        var p2 = SuperintelligencePromptBuilder.BuildCouncil(siSeed, siDomains, avgAtt, avgMed, domKey, profile);
                        var r2 = RewritePromptBuilder.CleanReply(await c2.CompleteAsync(m2, p2.System, p2.User, ct));
                        if (!string.IsNullOrWhiteSpace(r2)) siCouncil = r2;
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio offline — used built-in research council.");
                }
                File.WriteAllText(Path.Combine(OutputDirectory, "research_council_report.md"), siCouncil);

                // LM 3 — research report -> .docx
                string siReport = SuperintelligenceContent.DefaultResearchReportMarkdown(siDash, siDomains, siWords, avgAtt, avgMed, domKey);
                try
                {
                    Status?.Invoke("Writing the superintelligence research report with LM Studio…");
                    using var c3 = new LmStudioClient(_config.LmStudioUrl);
                    string m3 = _config.Model;
                    if (string.IsNullOrWhiteSpace(m3)) m3 = await c3.GetFirstModelAsync(ct) ?? "";
                    if (!string.IsNullOrWhiteSpace(m3))
                    {
                        var p3 = SuperintelligencePromptBuilder.BuildResearchReport(siSeed, seconds, siDomains, avgAtt, avgMed, domKey, profile);
                        var r3 = RewritePromptBuilder.CleanReply(await c3.CompleteAsync(m3, p3.System, p3.User, ct));
                        if (!string.IsNullOrWhiteSpace(r3)) siReport = r3;
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio offline — used built-in research report.");
                }
                DocxArticleWriter.Write(siReport, Path.Combine(OutputDirectory, "superintelligence_research_report.docx"), siFont);

                // LM 4 — 12-slide deck (accept only a full 12-slide LM deck)
                var siDeck = SuperintelligenceContent.DefaultDeck(siDash, siDomains, siWords, avgAtt, avgMed, domKey);
                try
                {
                    Status?.Invoke("Building the 12-slide deck with LM Studio…");
                    using var c4 = new LmStudioClient(_config.LmStudioUrl);
                    string m4 = _config.Model;
                    if (string.IsNullOrWhiteSpace(m4)) m4 = await c4.GetFirstModelAsync(ct) ?? "";
                    if (!string.IsNullOrWhiteSpace(m4))
                    {
                        var p4 = SuperintelligencePromptBuilder.BuildSlides(siDomains, avgAtt, avgMed, domKey);
                        var r4 = RewritePromptBuilder.CleanReply(await c4.CompleteAsync(m4, p4.System, p4.User, ct));
                        var parsed = PptxArticleWriter.ParseSlides(r4, 12);
                        if (parsed.Count == 12) siDeck = parsed; // else keep the deterministic 12-slide deck
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio offline — used built-in 12-slide deck.");
                }
                PptxArticleWriter.Write(siDeck, Path.Combine(OutputDirectory, "superintelligence_analysis.pptx"), siFont);

                Result?.Invoke(SuperintelligenceContent.Scorecard(siDash) + "\n\n" + siProblem);
                Status?.Invoke($"Saved your Superintelligence package to {OutputDirectory}.");
                Completed?.Invoke(Path.Combine(OutputDirectory, "superintelligence_research_report.docx"));
                return;
            }

            if (supervised)
            {
                Directory.CreateDirectory(OutputDirectory);
                var svWords = accumulator.Words;
                string svSeed = accumulator.Seed();
                int svDistinct = svWords.Distinct(StringComparer.OrdinalIgnoreCase).Count();
                var dataDir = Path.Combine(AppContext.BaseDirectory, "data");

                // count prior sessions (for population analysis) before adding this run's snapshot
                int svPrior = 0;
                try { svPrior = Directory.GetFiles(OutputDirectory, "recorded_eeg_*.csv").Length; } catch { /* none */ }

                // recorded_eeg.csv (+ timestamped snapshot for multi-session datasets) + translated_eeg.txt
                File.WriteAllText(Path.Combine(OutputDirectory, "recorded_eeg.csv"), NlpContent.RecordedEegCsv(svWords));
                var svStamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                File.WriteAllText(Path.Combine(OutputDirectory, $"recorded_eeg_{svStamp}.csv"), NlpContent.RecordedEegCsv(svWords));
                string svStats =
                    "# Translated EEG\n" +
                    $"recorded_seconds: {seconds}\n" +
                    $"words: {accumulator.Count}\n" +
                    $"distinct_words: {svDistinct}\n" +
                    $"signal_quality: {(svWords.Count > 0 ? "good" : "no signal")}\n" +
                    $"avg_attention: {avgAtt:0}\n" +
                    $"avg_meditation: {avgMed:0}\n" +
                    $"dominant_band: {domKey}\n\n";
                File.WriteAllText(Path.Combine(OutputDirectory, "translated_eeg.txt"), NlpContent.TranslatedText(svStats, svWords));

                // deterministic supervised pipeline (offline-safe)
                var svCareers = SupervisedCareers.DetectFromFile(dataDir, svWords);
                string svPredicted = SupervisedProfile.PredictLabel(avgAtt, avgMed, bandReadings, svWords);
                string svTopCareer = svCareers.Count > 0 ? svCareers[0].Career : "Research";

                File.WriteAllText(Path.Combine(OutputDirectory, "label_database.csv"), SupervisedContent.LabelDatabaseCsv());
                string svDataset = SupervisedContent.TrainingDatasetCsv(avgAtt, avgMed, bandReadings, svWords, domKey);
                File.WriteAllText(Path.Combine(OutputDirectory, "training_dataset.csv"), svDataset);
                int svDatasetRows = svDataset.Replace("\r\n", "\n").Split('\n').Count(l => l.Contains(',')) - 1;
                File.WriteAllText(Path.Combine(OutputDirectory, "feature_vectors.csv"), SupervisedProfile.FeatureVectorsCsv(avgAtt, avgMed, bandReadings, domKey));
                File.WriteAllText(Path.Combine(OutputDirectory, "prediction_results.csv"), SupervisedProfile.PredictionResultsCsv(avgAtt, avgMed, bandReadings, svWords, domKey));
                File.WriteAllText(Path.Combine(OutputDirectory, "classification_results.csv"), SupervisedProfile.ClassificationResultsCsv(avgAtt, avgMed, bandReadings, svWords));
                string svEval = SupervisedProfile.ModelEvaluationCsv(avgAtt, avgMed, bandReadings, svWords);
                File.WriteAllText(Path.Combine(OutputDirectory, "model_evaluation.csv"), svEval);
                File.WriteAllText(Path.Combine(OutputDirectory, "learning_progress.csv"), SupervisedProfile.LearningProgressCsv(avgAtt, avgMed, bandReadings, svWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "skill_predictions.csv"), SupervisedProfile.SkillPredictionsCsv(avgAtt, avgMed, bandReadings, svWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "population_analysis.csv"), SupervisedProfile.PopulationAnalysisCsv(avgAtt, avgMed, bandReadings, svWords, svPrior));
                File.WriteAllText(Path.Combine(OutputDirectory, "career_classification.csv"), SupervisedContent.CareerClassificationCsv(svCareers));

                // simulated trained models (descriptor files)
                var svModelsDir = Path.Combine(OutputDirectory, "trained_models");
                Directory.CreateDirectory(svModelsDir);
                foreach (var (file, modelContent) in SupervisedContent.TrainedModelDescriptors(svEval, svPredicted, svWords))
                    File.WriteAllText(Path.Combine(svModelsDir, file), modelContent);

                // append-only logs: feedback, brain database, training history
                SupervisedProfile.AppendFeedback(Path.Combine(OutputDirectory, "feedback_history.csv"), svPredicted, "pending");
                SupervisedProfile.AppendBrainDatabase(Path.Combine(OutputDirectory, "brain_learning_database.csv"), avgAtt, avgMed, bandReadings, svWords, svTopCareer);
                SupervisedProfile.AppendTrainingHistory(Path.Combine(OutputDirectory, "training_history.csv"), avgAtt, avgMed, bandReadings, svWords);

                string svFont = string.IsNullOrWhiteSpace(_config.Font) ? "Verdana" : _config.Font;

                // LM 1 — two narrative sections (knowledge discovery, AI explanations)
                string svKnowledge = SupervisedContent.DefaultKnowledgeDiscovery(svWords, svCareers);
                string svExplain = SupervisedContent.DefaultAiExplanations(svPredicted, svWords);
                try
                {
                    Status?.Invoke("Explaining predictions with LM Studio…");
                    using var c1 = new LmStudioClient(_config.LmStudioUrl);
                    string m1 = _config.Model;
                    if (string.IsNullOrWhiteSpace(m1)) m1 = await c1.GetFirstModelAsync(ct) ?? "";
                    if (!string.IsNullOrWhiteSpace(m1))
                    {
                        var p1 = SupervisedPromptBuilder.BuildNarratives(svSeed, svCareers, svPredicted, avgAtt, avgMed, domKey, profile);
                        var r1 = RewritePromptBuilder.CleanReply(await c1.CompleteAsync(m1, p1.System, p1.User, ct));
                        var kd = NlpContent.ExtractTextSection(r1, "# KNOWLEDGE DISCOVERY", "# AI EXPLANATIONS");
                        var ax = NlpContent.ExtractTextSection(r1, "# AI EXPLANATIONS", null);
                        if (!string.IsNullOrWhiteSpace(kd)) svKnowledge = kd;
                        if (!string.IsNullOrWhiteSpace(ax)) svExplain = ax;
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio offline — used built-in discovery & explanations.");
                }
                File.WriteAllText(Path.Combine(OutputDirectory, "knowledge_discovery_report.txt"), svKnowledge);
                File.WriteAllText(Path.Combine(OutputDirectory, "ai_explanations.txt"), svExplain);

                // LM 2 — research report -> .docx
                string svReport = SupervisedContent.DefaultReportMarkdown(svEval, svCareers, svWords, avgAtt, avgMed, domKey, svPredicted, svDatasetRows);
                try
                {
                    Status?.Invoke("Writing the supervised-learning report with LM Studio…");
                    using var c2 = new LmStudioClient(_config.LmStudioUrl);
                    string m2 = _config.Model;
                    if (string.IsNullOrWhiteSpace(m2)) m2 = await c2.GetFirstModelAsync(ct) ?? "";
                    if (!string.IsNullOrWhiteSpace(m2))
                    {
                        var p2 = SupervisedPromptBuilder.BuildReport(svSeed, seconds, svCareers, svPredicted, avgAtt, avgMed, domKey, profile);
                        var r2 = RewritePromptBuilder.CleanReply(await c2.CompleteAsync(m2, p2.System, p2.User, ct));
                        if (!string.IsNullOrWhiteSpace(r2)) svReport = r2;
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio offline — used built-in report.");
                }
                DocxArticleWriter.Write(svReport, Path.Combine(OutputDirectory, "supervised_learning_report.docx"), svFont);

                // LM 3 — 10-slide deck (accept only a full 10-slide LM deck)
                var svDeck = SupervisedContent.DefaultDeck(svEval, svCareers, svWords, avgAtt, avgMed, domKey, svPredicted, svDatasetRows);
                try
                {
                    Status?.Invoke("Building the 10-slide deck with LM Studio…");
                    using var c3 = new LmStudioClient(_config.LmStudioUrl);
                    string m3 = _config.Model;
                    if (string.IsNullOrWhiteSpace(m3)) m3 = await c3.GetFirstModelAsync(ct) ?? "";
                    if (!string.IsNullOrWhiteSpace(m3))
                    {
                        var p3 = SupervisedPromptBuilder.BuildSlides(svCareers, svPredicted, avgAtt, avgMed, domKey);
                        var r3 = RewritePromptBuilder.CleanReply(await c3.CompleteAsync(m3, p3.System, p3.User, ct));
                        var parsed = PptxArticleWriter.ParseSlides(r3, 10);
                        if (parsed.Count == 10) svDeck = parsed; // else keep the deterministic 10-slide deck
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio offline — used built-in 10-slide deck.");
                }
                PptxArticleWriter.Write(svDeck, Path.Combine(OutputDirectory, "supervised_learning_analysis.pptx"), svFont);

                Result?.Invoke(
                    SupervisedContent.Scorecard(svEval, SupervisedProfile.FocusScore(avgAtt, bandReadings),
                        SupervisedProfile.CreativityScore(bandReadings, svWords), SupervisedProfile.ProductivityScore(avgAtt, bandReadings),
                        svPredicted, svTopCareer) + "\n\n" + svKnowledge);
                Status?.Invoke($"Saved your Supervised Learning package to {OutputDirectory}.");
                Completed?.Invoke(Path.Combine(OutputDirectory, "supervised_learning_report.docx"));
                return;
            }

            if (swarm)
            {
                Directory.CreateDirectory(OutputDirectory);
                var swWords = accumulator.Words;
                string swSeed = accumulator.Seed();
                int swDistinct = swWords.Distinct(StringComparer.OrdinalIgnoreCase).Count();
                var dataDir = Path.Combine(AppContext.BaseDirectory, "data");

                int swPrior = 0;
                try { swPrior = Directory.GetFiles(OutputDirectory, "recorded_eeg_*.csv").Length; } catch { /* none */ }

                // recorded_eeg.csv (+ timestamped snapshot for multi-user/multi-session) + translated_eeg.txt
                File.WriteAllText(Path.Combine(OutputDirectory, "recorded_eeg.csv"), NlpContent.RecordedEegCsv(swWords));
                var swStamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                File.WriteAllText(Path.Combine(OutputDirectory, $"recorded_eeg_{swStamp}.csv"), NlpContent.RecordedEegCsv(swWords));
                string swStatsHdr =
                    "# Translated EEG\n" +
                    $"recorded_seconds: {seconds}\n" +
                    $"words: {accumulator.Count}\n" +
                    $"distinct_words: {swDistinct}\n" +
                    $"signal_quality: {(swWords.Count > 0 ? "good" : "no signal")}\n" +
                    $"avg_attention: {avgAtt:0}\n" +
                    $"avg_meditation: {avgMed:0}\n" +
                    $"dominant_band: {domKey}\n\n";
                File.WriteAllText(Path.Combine(OutputDirectory, "translated_eeg.txt"), NlpContent.TranslatedText(swStatsHdr, swWords));

                // deterministic swarm pipeline (offline-safe)
                var swDomainObj = SwarmDomains.LoadFromDir(dataDir);
                var swRoles = swDomainObj.Detect(swWords);
                var swAgents = SwarmContent.BuildAgents(swWords, swDomainObj);
                var swDash = SwarmProfile.Dashboard(avgAtt, avgMed, bandReadings, swWords);
                string swTopRole = swRoles.Count > 0 ? swRoles[0].Domain : "Generalist";

                File.WriteAllText(Path.Combine(OutputDirectory, "swarm_agents.csv"), SwarmContent.SwarmAgentsCsv(swAgents));
                File.WriteAllText(Path.Combine(OutputDirectory, "swarm_network.csv"), SwarmContent.SwarmNetworkCsv(swAgents));
                File.WriteAllText(Path.Combine(OutputDirectory, "idea_colony.csv"), SwarmContent.IdeaColonyCsv(swWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "knowledge_swarm.csv"), SwarmContent.KnowledgeSwarmCsv(swWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "swarm_roles.csv"), SwarmContent.SwarmRolesCsv(swWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "consensus_analysis.csv"), SwarmProfile.ConsensusAnalysisCsv(avgAtt, avgMed, bandReadings, swWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "swarm_comparison.csv"), SwarmProfile.SwarmComparisonCsv(avgAtt, avgMed, bandReadings, swWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "knowledge_ecosystem.md"), SwarmContent.KnowledgeEcosystemMd(swWords, swRoles));
                File.WriteAllText(Path.Combine(OutputDirectory, "global_noosphere.csv"), SwarmContent.GlobalNoosphereCsv(swWords, swDomainObj));
                var swShared = NlpContent.TopWords(swWords, 5);
                File.WriteAllText(Path.Combine(OutputDirectory, "multiuser_swarm.csv"), SwarmProfile.MultiUserSwarmCsv(avgAtt, avgMed, bandReadings, swWords, swShared, swPrior));

                // swarm memory database
                SwarmProfile.AppendHistory(Path.Combine(OutputDirectory, "swarm_history.csv"), avgAtt, avgMed, bandReadings, swWords, swAgents.Count, swTopRole);

                string swFont = string.IsNullOrWhiteSpace(_config.Font) ? "Verdana" : _config.Font;

                // LM 1 — four narrative sections (collective intelligence, emergent behavior, innovation swarm, forecast)
                string swCollective = SwarmContent.DefaultCollectiveIntelligence(swWords, swRoles);
                string swEmergent = SwarmContent.DefaultEmergentBehavior(swWords);
                string swInnovation = SwarmContent.DefaultInnovationSwarm(swRoles, swWords);
                string swForecast = SwarmContent.DefaultSwarmForecast(swRoles);
                try
                {
                    Status?.Invoke("Simulating the swarm with LM Studio…");
                    using var c1 = new LmStudioClient(_config.LmStudioUrl);
                    string m1 = _config.Model;
                    if (string.IsNullOrWhiteSpace(m1)) m1 = await c1.GetFirstModelAsync(ct) ?? "";
                    if (!string.IsNullOrWhiteSpace(m1))
                    {
                        var p1 = SwarmPromptBuilder.BuildNarratives(swSeed, swRoles, swAgents.Count, avgAtt, avgMed, domKey, profile);
                        var r1 = RewritePromptBuilder.CleanReply(await c1.CompleteAsync(m1, p1.System, p1.User, ct));
                        var ci = NlpContent.ExtractTextSection(r1, "# COLLECTIVE INTELLIGENCE", "# EMERGENT BEHAVIOR");
                        var eb = NlpContent.ExtractTextSection(r1, "# EMERGENT BEHAVIOR", "# INNOVATION SWARM");
                        var inv = NlpContent.ExtractTextSection(r1, "# INNOVATION SWARM", "# SWARM FORECAST");
                        var fc = NlpContent.ExtractTextSection(r1, "# SWARM FORECAST", null);
                        if (!string.IsNullOrWhiteSpace(ci)) swCollective = ci;
                        if (!string.IsNullOrWhiteSpace(eb)) swEmergent = eb;
                        if (!string.IsNullOrWhiteSpace(inv)) swInnovation = inv;
                        if (!string.IsNullOrWhiteSpace(fc)) swForecast = fc;
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio offline — used built-in swarm narratives.");
                }
                File.WriteAllText(Path.Combine(OutputDirectory, "collective_intelligence_report.txt"), swCollective);
                File.WriteAllText(Path.Combine(OutputDirectory, "emergent_behavior.txt"), swEmergent);
                File.WriteAllText(Path.Combine(OutputDirectory, "innovation_swarm_report.txt"), swInnovation);
                File.WriteAllText(Path.Combine(OutputDirectory, "swarm_forecast.txt"), swForecast);

                // LM 2 — distributed problem solving -> .docx
                string swSolution = SwarmContent.DefaultSolutionMarkdown(swRoles, swWords);
                try
                {
                    Status?.Invoke("Solving the challenge with the swarm via LM Studio…");
                    using var c2 = new LmStudioClient(_config.LmStudioUrl);
                    string m2 = _config.Model;
                    if (string.IsNullOrWhiteSpace(m2)) m2 = await c2.GetFirstModelAsync(ct) ?? "";
                    if (!string.IsNullOrWhiteSpace(m2))
                    {
                        var p2 = SwarmPromptBuilder.BuildSolution(swSeed, swRoles, avgAtt, avgMed, domKey, profile);
                        var r2 = RewritePromptBuilder.CleanReply(await c2.CompleteAsync(m2, p2.System, p2.User, ct));
                        if (!string.IsNullOrWhiteSpace(r2)) swSolution = r2;
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio offline — used built-in swarm solution.");
                }
                DocxArticleWriter.Write(swSolution, Path.Combine(OutputDirectory, "swarm_solution_report.docx"), swFont);

                // LM 3 — research paper -> .docx
                string swResearch = SwarmContent.DefaultResearchMarkdown(swDash, swRoles, swWords, avgAtt, avgMed, domKey, swAgents.Count);
                try
                {
                    Status?.Invoke("Writing the swarm research paper with LM Studio…");
                    using var c3 = new LmStudioClient(_config.LmStudioUrl);
                    string m3 = _config.Model;
                    if (string.IsNullOrWhiteSpace(m3)) m3 = await c3.GetFirstModelAsync(ct) ?? "";
                    if (!string.IsNullOrWhiteSpace(m3))
                    {
                        var p3 = SwarmPromptBuilder.BuildResearch(swSeed, seconds, swRoles, swAgents.Count, avgAtt, avgMed, domKey, profile);
                        var r3 = RewritePromptBuilder.CleanReply(await c3.CompleteAsync(m3, p3.System, p3.User, ct));
                        if (!string.IsNullOrWhiteSpace(r3)) swResearch = r3;
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio offline — used built-in research paper.");
                }
                DocxArticleWriter.Write(swResearch, Path.Combine(OutputDirectory, "swarm_intelligence_research.docx"), swFont);

                // LM 4 — 10-slide deck (accept only a full 10-slide LM deck)
                var swDeck = SwarmContent.DefaultDeck(swDash, swRoles, swWords, avgAtt, avgMed, domKey, swAgents.Count);
                try
                {
                    Status?.Invoke("Building the 10-slide deck with LM Studio…");
                    using var c4 = new LmStudioClient(_config.LmStudioUrl);
                    string m4 = _config.Model;
                    if (string.IsNullOrWhiteSpace(m4)) m4 = await c4.GetFirstModelAsync(ct) ?? "";
                    if (!string.IsNullOrWhiteSpace(m4))
                    {
                        var p4 = SwarmPromptBuilder.BuildSlides(swRoles, swAgents.Count, avgAtt, avgMed, domKey);
                        var r4 = RewritePromptBuilder.CleanReply(await c4.CompleteAsync(m4, p4.System, p4.User, ct));
                        var parsed = PptxArticleWriter.ParseSlides(r4, 10);
                        if (parsed.Count == 10) swDeck = parsed; // else keep the deterministic 10-slide deck
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio offline — used built-in 10-slide deck.");
                }
                PptxArticleWriter.Write(swDeck, Path.Combine(OutputDirectory, "swarm_intelligence_analysis.pptx"), swFont);

                Result?.Invoke(SwarmContent.Scorecard(swDash, swAgents.Count, swTopRole) + "\n\n" + swCollective);
                Status?.Invoke($"Saved your Swarm Intelligence package to {OutputDirectory}.");
                Completed?.Invoke(Path.Combine(OutputDirectory, "swarm_intelligence_research.docx"));
                return;
            }

            if (taskauto)
            {
                Directory.CreateDirectory(OutputDirectory);
                var taWords = accumulator.Words;
                string taSeed = accumulator.Seed();
                int taDistinct = taWords.Distinct(StringComparer.OrdinalIgnoreCase).Count();
                var dataDir = Path.Combine(AppContext.BaseDirectory, "data");

                // recorded_eeg.csv (+ timestamped snapshot) + translated_eeg.txt
                File.WriteAllText(Path.Combine(OutputDirectory, "recorded_eeg.csv"), NlpContent.RecordedEegCsv(taWords));
                var taStamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                File.WriteAllText(Path.Combine(OutputDirectory, $"recorded_eeg_{taStamp}.csv"), NlpContent.RecordedEegCsv(taWords));
                string taStatsHdr =
                    "# Translated EEG\n" +
                    $"recorded_seconds: {seconds}\n" +
                    $"words: {accumulator.Count}\n" +
                    $"distinct_words: {taDistinct}\n" +
                    $"signal_quality: {(taWords.Count > 0 ? "good" : "no signal")}\n" +
                    $"avg_attention: {avgAtt:0}\n" +
                    $"avg_meditation: {avgMed:0}\n" +
                    $"dominant_band: {domKey}\n\n";
                File.WriteAllText(Path.Combine(OutputDirectory, "translated_eeg.txt"), NlpContent.TranslatedText(taStatsHdr, taWords));

                // deterministic task pipeline (offline-safe)
                var taCatObj = TaskCategories.LoadFromDir(dataDir);
                var taCategories = taCatObj.Detect(taWords);
                var taTasks = TaskContent.BuildTasks(taWords, taCatObj);
                var taScores = TaskProfile.Scores(avgAtt, avgMed, bandReadings, taWords);
                string taTopCategory = taCategories.Count > 0 ? taCategories[0].Category : "Personal Development";
                var (taActive, taCompleted, taDelayed, taFailed) = TaskContent.StatusCounts(taTasks.Count);

                File.WriteAllText(Path.Combine(OutputDirectory, "identified_tasks.csv"), TaskContent.IdentifiedTasksCsv(taTasks));
                File.WriteAllText(Path.Combine(OutputDirectory, "task_categories.csv"), TaskContent.TaskCategoriesCsv(taCategories));
                File.WriteAllText(Path.Combine(OutputDirectory, "task_priorities.csv"), TaskContent.TaskPrioritiesCsv(taTasks));
                File.WriteAllText(Path.Combine(OutputDirectory, "workflow_definitions.md"), TaskContent.WorkflowDefinitionsMd(taTasks));
                File.WriteAllText(Path.Combine(OutputDirectory, "project_breakdown.csv"), TaskContent.ProjectBreakdownCsv(taTasks));
                File.WriteAllText(Path.Combine(OutputDirectory, "task_schedule.csv"), TaskContent.TaskScheduleCsv(taTasks));
                File.WriteAllText(Path.Combine(OutputDirectory, "productivity_metrics.csv"), TaskProfile.ProductivityMetricsCsv(avgAtt, avgMed, bandReadings, taWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "task_tracking.csv"), TaskContent.TaskTrackingCsv(taTasks));
                File.WriteAllText(Path.Combine(OutputDirectory, "virtual_workforce.csv"), TaskContent.VirtualWorkforceCsv(taTasks));

                // automation scripts
                var taScriptsDir = Path.Combine(OutputDirectory, "automation_scripts");
                Directory.CreateDirectory(taScriptsDir);
                foreach (var (file, scriptContent) in TaskContent.AutomationScripts(taTasks))
                    File.WriteAllText(Path.Combine(taScriptsDir, file), scriptContent);

                // historical task learning
                TaskProfile.AppendHistory(Path.Combine(OutputDirectory, "task_history.csv"), avgAtt, avgMed, bandReadings, taWords, taTasks.Count, taCompleted, taTopCategory);

                string taFont = string.IsNullOrWhiteSpace(_config.Font) ? "Verdana" : _config.Font;

                // LM 1 — three narrative sections (recommendations, agent team, automation plans)
                string taRecs = TaskContent.DefaultRecommendations(taTasks);
                string taAgents = TaskContent.DefaultAgentTeam(taTasks);
                string taPlans = TaskContent.DefaultAutomationPlans(taTasks);
                try
                {
                    Status?.Invoke("Planning tasks with LM Studio…");
                    using var c1 = new LmStudioClient(_config.LmStudioUrl);
                    string m1 = _config.Model;
                    if (string.IsNullOrWhiteSpace(m1)) m1 = await c1.GetFirstModelAsync(ct) ?? "";
                    if (!string.IsNullOrWhiteSpace(m1))
                    {
                        var p1 = TaskPromptBuilder.BuildNarratives(taSeed, taCategories, taTasks.Count, avgAtt, avgMed, domKey, profile);
                        var r1 = RewritePromptBuilder.CleanReply(await c1.CompleteAsync(m1, p1.System, p1.User, ct));
                        var rc = NlpContent.ExtractTextSection(r1, "# TASK RECOMMENDATIONS", "# AGENT TEAM");
                        var ag = NlpContent.ExtractTextSection(r1, "# AGENT TEAM", "# AUTOMATION PLANS");
                        var pl = NlpContent.ExtractTextSection(r1, "# AUTOMATION PLANS", null);
                        if (!string.IsNullOrWhiteSpace(rc)) taRecs = rc;
                        if (!string.IsNullOrWhiteSpace(ag)) taAgents = ag;
                        if (!string.IsNullOrWhiteSpace(pl)) taPlans = pl;
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio offline — used built-in task plans.");
                }
                File.WriteAllText(Path.Combine(OutputDirectory, "task_recommendations.txt"), taRecs);
                File.WriteAllText(Path.Combine(OutputDirectory, "agent_team.md"), taAgents);
                File.WriteAllText(Path.Combine(OutputDirectory, "automation_plans.md"), taPlans);

                // LM 2 — AI project manager report -> .docx
                string taPm = TaskContent.DefaultProjectManagerMarkdown(taTasks, taScores);
                try
                {
                    Status?.Invoke("Acting as project manager with LM Studio…");
                    using var c2 = new LmStudioClient(_config.LmStudioUrl);
                    string m2 = _config.Model;
                    if (string.IsNullOrWhiteSpace(m2)) m2 = await c2.GetFirstModelAsync(ct) ?? "";
                    if (!string.IsNullOrWhiteSpace(m2))
                    {
                        var p2 = TaskPromptBuilder.BuildProjectManager(taSeed, taCategories, taTasks.Count, avgAtt, avgMed, domKey, profile);
                        var r2 = RewritePromptBuilder.CleanReply(await c2.CompleteAsync(m2, p2.System, p2.User, ct));
                        if (!string.IsNullOrWhiteSpace(r2)) taPm = r2;
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio offline — used built-in project-manager report.");
                }
                DocxArticleWriter.Write(taPm, Path.Combine(OutputDirectory, "project_manager_report.docx"), taFont);

                // LM 3 — automation report -> .docx
                string taReport = TaskContent.DefaultReportMarkdown(taTasks, taCategories, taScores, avgAtt, avgMed, domKey);
                try
                {
                    Status?.Invoke("Writing the task-automation report with LM Studio…");
                    using var c3 = new LmStudioClient(_config.LmStudioUrl);
                    string m3 = _config.Model;
                    if (string.IsNullOrWhiteSpace(m3)) m3 = await c3.GetFirstModelAsync(ct) ?? "";
                    if (!string.IsNullOrWhiteSpace(m3))
                    {
                        var p3 = TaskPromptBuilder.BuildReport(taSeed, seconds, taCategories, taTasks.Count, avgAtt, avgMed, domKey, profile);
                        var r3 = RewritePromptBuilder.CleanReply(await c3.CompleteAsync(m3, p3.System, p3.User, ct));
                        if (!string.IsNullOrWhiteSpace(r3)) taReport = r3;
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio offline — used built-in automation report.");
                }
                DocxArticleWriter.Write(taReport, Path.Combine(OutputDirectory, "task_automation_report.docx"), taFont);

                // LM 4 — 10-slide deck (accept only a full 10-slide LM deck)
                var taDeck = TaskContent.DefaultDeck(taTasks, taCategories, taScores, avgAtt, avgMed, domKey);
                try
                {
                    Status?.Invoke("Building the 10-slide deck with LM Studio…");
                    using var c4 = new LmStudioClient(_config.LmStudioUrl);
                    string m4 = _config.Model;
                    if (string.IsNullOrWhiteSpace(m4)) m4 = await c4.GetFirstModelAsync(ct) ?? "";
                    if (!string.IsNullOrWhiteSpace(m4))
                    {
                        var p4 = TaskPromptBuilder.BuildSlides(taCategories, taTasks.Count, avgAtt, avgMed, domKey);
                        var r4 = RewritePromptBuilder.CleanReply(await c4.CompleteAsync(m4, p4.System, p4.User, ct));
                        var parsed = PptxArticleWriter.ParseSlides(r4, 10);
                        if (parsed.Count == 10) taDeck = parsed; // else keep the deterministic 10-slide deck
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio offline — used built-in 10-slide deck.");
                }
                PptxArticleWriter.Write(taDeck, Path.Combine(OutputDirectory, "task_automation_analysis.pptx"), taFont);

                Result?.Invoke(TaskContent.Scorecard(taScores, taTasks.Count, taActive, taCompleted) + "\n\n" + taRecs);
                Status?.Invoke($"Saved your Task Automation package to {OutputDirectory}.");
                Completed?.Invoke(Path.Combine(OutputDirectory, "task_automation_report.docx"));
                return;
            }

            if (tom)
            {
                Directory.CreateDirectory(OutputDirectory);
                var tmWords = accumulator.Words;
                string tmSeed = accumulator.Seed();
                int tmDistinct = tmWords.Distinct(StringComparer.OrdinalIgnoreCase).Count();
                var dataDir = Path.Combine(AppContext.BaseDirectory, "data");

                int tmPrior = 0;
                try { tmPrior = Directory.GetFiles(OutputDirectory, "recorded_eeg_*.csv").Length; } catch { /* none */ }

                // recorded_eeg.csv (+ timestamped snapshot for multi-session comparison) + translated_eeg.txt
                File.WriteAllText(Path.Combine(OutputDirectory, "recorded_eeg.csv"), NlpContent.RecordedEegCsv(tmWords));
                var tmStamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                File.WriteAllText(Path.Combine(OutputDirectory, $"recorded_eeg_{tmStamp}.csv"), NlpContent.RecordedEegCsv(tmWords));
                string tmStatsHdr =
                    "# Translated EEG\n" +
                    $"recorded_seconds: {seconds}\n" +
                    $"words: {accumulator.Count}\n" +
                    $"distinct_words: {tmDistinct}\n" +
                    $"signal_quality: {(tmWords.Count > 0 ? "good" : "no signal")}\n" +
                    $"avg_attention: {avgAtt:0}\n" +
                    $"avg_meditation: {avgMed:0}\n" +
                    $"dominant_band: {domKey}\n\n";
                File.WriteAllText(Path.Combine(OutputDirectory, "translated_eeg.txt"), NlpContent.TranslatedText(tmStatsHdr, tmWords));

                // deterministic Theory of Mind pipeline (offline-safe; all inferences are hypotheses)
                var tmIntents = TomIntents.DetectFromFile(dataDir, tmWords);
                var tmDash = TomProfile.Dashboard(avgAtt, avgMed, bandReadings, tmWords);
                string tmTopIntent = tmIntents.Count > 0 ? tmIntents[0].Intent : "Learning";

                File.WriteAllText(Path.Combine(OutputDirectory, "theory_of_mind_profile.csv"), TomProfile.ProfileCsv(avgAtt, avgMed, bandReadings, tmWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "perspective_analysis.csv"), TomProfile.PerspectiveAnalysisCsv(avgAtt, avgMed, bandReadings, tmWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "intent_model.csv"), TomContent.IntentModelCsv(tmIntents));
                File.WriteAllText(Path.Combine(OutputDirectory, "belief_structure.md"), TomContent.BeliefStructureMd(tmWords, tmIntents));
                File.WriteAllText(Path.Combine(OutputDirectory, "goal_predictions.csv"), TomContent.GoalPredictionsCsv(tmIntents, tmWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "decision_style.csv"), TomProfile.DecisionStyleCsv(avgAtt, avgMed, bandReadings, tmWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "social_cognition.csv"), TomProfile.SocialCognitionCsv(avgAtt, avgMed, bandReadings, tmWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "perspective_taking.csv"), TomProfile.PerspectiveTakingCsv(avgAtt, avgMed, bandReadings, tmWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "theory_of_mind_graph.md"), TomContent.TheoryOfMindGraphMd(tmWords, tmIntents));
                File.WriteAllText(Path.Combine(OutputDirectory, "human_ai_perspective_comparison.csv"), TomProfile.HumanAiComparisonCsv(tmWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "theory_of_mind_trends.csv"), TomProfile.TrendsCsv(tmPrior));

                // long-term cognitive development
                TomProfile.AppendHistory(Path.Combine(OutputDirectory, "theory_of_mind_history.csv"), avgAtt, avgMed, bandReadings, tmWords, tmTopIntent);

                string tmFont = string.IsNullOrWhiteSpace(_config.Font) ? "Verdana" : _config.Font;

                // LM 1 — two narrative sections (perspective simulations, cognitive scenarios)
                string tmPerspectives = TomContent.DefaultPerspectiveSimulations(tmWords, tmIntents);
                string tmScenarios = TomContent.DefaultCognitiveScenarios(tmIntents, tmWords);
                try
                {
                    Status?.Invoke("Simulating perspectives with LM Studio…");
                    using var c1 = new LmStudioClient(_config.LmStudioUrl);
                    string m1 = _config.Model;
                    if (string.IsNullOrWhiteSpace(m1)) m1 = await c1.GetFirstModelAsync(ct) ?? "";
                    if (!string.IsNullOrWhiteSpace(m1))
                    {
                        var p1 = TomPromptBuilder.BuildNarratives(tmSeed, tmIntents, avgAtt, avgMed, domKey, profile);
                        var r1 = RewritePromptBuilder.CleanReply(await c1.CompleteAsync(m1, p1.System, p1.User, ct));
                        var ps = NlpContent.ExtractTextSection(r1, "# PERSPECTIVE SIMULATIONS", "# COGNITIVE SCENARIOS");
                        var cs = NlpContent.ExtractTextSection(r1, "# COGNITIVE SCENARIOS", null);
                        if (!string.IsNullOrWhiteSpace(ps)) tmPerspectives = ps;
                        if (!string.IsNullOrWhiteSpace(cs)) tmScenarios = cs;
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio offline — used built-in perspective simulations.");
                }
                File.WriteAllText(Path.Combine(OutputDirectory, "perspective_simulations.txt"), tmPerspectives);
                File.WriteAllText(Path.Combine(OutputDirectory, "cognitive_scenarios.txt"), tmScenarios);

                // LM 2 — research report -> .docx
                string tmReport = TomContent.DefaultReportMarkdown(tmDash, tmIntents, tmWords, avgAtt, avgMed, domKey);
                try
                {
                    Status?.Invoke("Writing the Theory of Mind report with LM Studio…");
                    using var c2 = new LmStudioClient(_config.LmStudioUrl);
                    string m2 = _config.Model;
                    if (string.IsNullOrWhiteSpace(m2)) m2 = await c2.GetFirstModelAsync(ct) ?? "";
                    if (!string.IsNullOrWhiteSpace(m2))
                    {
                        var p2 = TomPromptBuilder.BuildReport(tmSeed, seconds, tmIntents, avgAtt, avgMed, domKey, profile);
                        var r2 = RewritePromptBuilder.CleanReply(await c2.CompleteAsync(m2, p2.System, p2.User, ct));
                        if (!string.IsNullOrWhiteSpace(r2)) tmReport = r2;
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio offline — used built-in report.");
                }
                DocxArticleWriter.Write(tmReport, Path.Combine(OutputDirectory, "theory_of_mind_report.docx"), tmFont);

                // LM 3 — 10-slide deck (accept only a full 10-slide LM deck)
                var tmDeck = TomContent.DefaultDeck(tmDash, tmIntents, tmWords, avgAtt, avgMed, domKey);
                try
                {
                    Status?.Invoke("Building the 10-slide deck with LM Studio…");
                    using var c3 = new LmStudioClient(_config.LmStudioUrl);
                    string m3 = _config.Model;
                    if (string.IsNullOrWhiteSpace(m3)) m3 = await c3.GetFirstModelAsync(ct) ?? "";
                    if (!string.IsNullOrWhiteSpace(m3))
                    {
                        var p3 = TomPromptBuilder.BuildSlides(tmIntents, avgAtt, avgMed, domKey);
                        var r3 = RewritePromptBuilder.CleanReply(await c3.CompleteAsync(m3, p3.System, p3.User, ct));
                        var parsed = PptxArticleWriter.ParseSlides(r3, 10);
                        if (parsed.Count == 10) tmDeck = parsed; // else keep the deterministic 10-slide deck
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio offline — used built-in 10-slide deck.");
                }
                PptxArticleWriter.Write(tmDeck, Path.Combine(OutputDirectory, "theory_of_mind_analysis.pptx"), tmFont);

                Result?.Invoke(TomContent.Scorecard(tmDash, tmTopIntent) + "\n\n" + tmPerspectives);
                Status?.Invoke($"Saved your Theory of Mind package to {OutputDirectory}.");
                Completed?.Invoke(Path.Combine(OutputDirectory, "theory_of_mind_report.docx"));
                return;
            }

            if (transfer)
            {
                Directory.CreateDirectory(OutputDirectory);
                var trWords = accumulator.Words;
                string trSeed = accumulator.Seed();
                int trDistinct = trWords.Distinct(StringComparer.OrdinalIgnoreCase).Count();
                var dataDir = Path.Combine(AppContext.BaseDirectory, "data");

                int trPrior = 0;
                try { trPrior = Directory.GetFiles(OutputDirectory, "recorded_eeg_*.csv").Length; } catch { /* none */ }

                // recorded_eeg.csv (+ timestamped snapshot for batch/multi-session) + translated_eeg.txt
                File.WriteAllText(Path.Combine(OutputDirectory, "recorded_eeg.csv"), NlpContent.RecordedEegCsv(trWords));
                var trStamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                File.WriteAllText(Path.Combine(OutputDirectory, $"recorded_eeg_{trStamp}.csv"), NlpContent.RecordedEegCsv(trWords));
                string trStatsHdr =
                    "# Translated EEG\n" +
                    $"recorded_seconds: {seconds}\n" +
                    $"words: {accumulator.Count}\n" +
                    $"distinct_words: {trDistinct}\n" +
                    $"signal_quality: {(trWords.Count > 0 ? "good" : "no signal")}\n" +
                    $"avg_attention: {avgAtt:0}\n" +
                    $"avg_meditation: {avgMed:0}\n" +
                    $"dominant_band: {domKey}\n\n";
                File.WriteAllText(Path.Combine(OutputDirectory, "translated_eeg.txt"), NlpContent.TranslatedText(trStatsHdr, trWords));

                // deterministic transfer-learning pipeline (offline-safe)
                var trDomainObj = TransferDomains.LoadFromDir(dataDir);
                var trDomains = trDomainObj.Detect(trWords);
                int trActive = trDomains.Count(d => d.Count > 0);
                var trDash = TransferProfile.Dashboard(avgAtt, avgMed, bandReadings, trWords, trActive);
                var trSkills = TransferProfile.SkillTransferScores(avgAtt, avgMed, bandReadings, trWords);
                string trTopDomain = trDomains.Count > 0 ? trDomains[0].Domain : "General";

                File.WriteAllText(Path.Combine(OutputDirectory, "knowledge_profile.csv"), TransferContent.KnowledgeProfileCsv(trWords, trDomains, trDomainObj));
                File.WriteAllText(Path.Combine(OutputDirectory, "transfer_learning_map.csv"), TransferContent.TransferLearningMapCsv(trDomains));
                File.WriteAllText(Path.Combine(OutputDirectory, "concept_transfer_graph.md"), TransferContent.ConceptTransferGraphMd(trWords, trDomains));
                File.WriteAllText(Path.Combine(OutputDirectory, "cross_domain_expansion.csv"), TransferContent.CrossDomainExpansionCsv(trDomains, trWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "learning_acceleration.csv"), TransferContent.LearningAccelerationCsv(trDomains));
                File.WriteAllText(Path.Combine(OutputDirectory, "cognitive_adaptation.csv"), TransferProfile.CognitiveAdaptationCsv(avgAtt, avgMed, bandReadings, trWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "human_ai_transfer_comparison.csv"), TransferProfile.HumanAiTransferCsv(trWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "career_transfer_analysis.csv"), TransferContent.CareerTransferCsv(trDomains));
                File.WriteAllText(Path.Combine(OutputDirectory, "transfer_projects.md"), TransferContent.TransferProjectsMd(trWords, trDomains));
                File.WriteAllText(Path.Combine(OutputDirectory, "learning_evolution.csv"), TransferProfile.LearningEvolutionCsv(avgAtt, avgMed, bandReadings, trWords, trPrior));

                // transfer-learning memory database
                TransferProfile.AppendHistory(Path.Combine(OutputDirectory, "transfer_learning_history.csv"), avgAtt, avgMed, bandReadings, trWords, trActive, trTopDomain);

                string trFont = string.IsNullOrWhiteSpace(_config.Font) ? "Verdana" : _config.Font;

                // LM 1 — four narrative sections (skill transfer, knowledge reuse, research transfer, innovation transfer)
                string trSkill = TransferContent.DefaultSkillTransfer(trSkills, trWords);
                string trReuse = TransferContent.DefaultKnowledgeReuse(trWords);
                string trResearch = TransferContent.DefaultResearchTransfer(trDomains, trWords);
                string trInnovation = TransferContent.DefaultInnovationTransfer(trDomains, trWords);
                try
                {
                    Status?.Invoke("Transferring knowledge with LM Studio…");
                    using var c1 = new LmStudioClient(_config.LmStudioUrl);
                    string m1 = _config.Model;
                    if (string.IsNullOrWhiteSpace(m1)) m1 = await c1.GetFirstModelAsync(ct) ?? "";
                    if (!string.IsNullOrWhiteSpace(m1))
                    {
                        var p1 = TransferPromptBuilder.BuildNarratives(trSeed, trDomains, avgAtt, avgMed, domKey, profile);
                        var r1 = RewritePromptBuilder.CleanReply(await c1.CompleteAsync(m1, p1.System, p1.User, ct));
                        var sk = NlpContent.ExtractTextSection(r1, "# SKILL TRANSFER", "# KNOWLEDGE REUSE");
                        var kr = NlpContent.ExtractTextSection(r1, "# KNOWLEDGE REUSE", "# RESEARCH TRANSFER");
                        var rt = NlpContent.ExtractTextSection(r1, "# RESEARCH TRANSFER", "# INNOVATION TRANSFER");
                        var it = NlpContent.ExtractTextSection(r1, "# INNOVATION TRANSFER", null);
                        if (!string.IsNullOrWhiteSpace(sk)) trSkill = sk;
                        if (!string.IsNullOrWhiteSpace(kr)) trReuse = kr;
                        if (!string.IsNullOrWhiteSpace(rt)) trResearch = rt;
                        if (!string.IsNullOrWhiteSpace(it)) trInnovation = it;
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio offline — used built-in transfer narratives.");
                }
                File.WriteAllText(Path.Combine(OutputDirectory, "skill_transfer_report.txt"), trSkill);
                File.WriteAllText(Path.Combine(OutputDirectory, "knowledge_reuse_report.txt"), trReuse);
                File.WriteAllText(Path.Combine(OutputDirectory, "research_transfer_opportunities.txt"), trResearch);
                File.WriteAllText(Path.Combine(OutputDirectory, "innovation_transfer_report.txt"), trInnovation);

                // LM 2 — research report -> .docx
                string trReport = TransferContent.DefaultReportMarkdown(trDash, trDomains, trWords, avgAtt, avgMed, domKey);
                try
                {
                    Status?.Invoke("Writing the transfer-learning report with LM Studio…");
                    using var c2 = new LmStudioClient(_config.LmStudioUrl);
                    string m2 = _config.Model;
                    if (string.IsNullOrWhiteSpace(m2)) m2 = await c2.GetFirstModelAsync(ct) ?? "";
                    if (!string.IsNullOrWhiteSpace(m2))
                    {
                        var p2 = TransferPromptBuilder.BuildReport(trSeed, seconds, trDomains, avgAtt, avgMed, domKey, profile);
                        var r2 = RewritePromptBuilder.CleanReply(await c2.CompleteAsync(m2, p2.System, p2.User, ct));
                        if (!string.IsNullOrWhiteSpace(r2)) trReport = r2;
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio offline — used built-in report.");
                }
                DocxArticleWriter.Write(trReport, Path.Combine(OutputDirectory, "transfer_learning_report.docx"), trFont);

                // LM 3 — 10-slide deck (accept only a full 10-slide LM deck)
                var trDeck = TransferContent.DefaultDeck(trDash, trDomains, trWords, avgAtt, avgMed, domKey);
                try
                {
                    Status?.Invoke("Building the 10-slide deck with LM Studio…");
                    using var c3 = new LmStudioClient(_config.LmStudioUrl);
                    string m3 = _config.Model;
                    if (string.IsNullOrWhiteSpace(m3)) m3 = await c3.GetFirstModelAsync(ct) ?? "";
                    if (!string.IsNullOrWhiteSpace(m3))
                    {
                        var p3 = TransferPromptBuilder.BuildSlides(trDomains, avgAtt, avgMed, domKey);
                        var r3 = RewritePromptBuilder.CleanReply(await c3.CompleteAsync(m3, p3.System, p3.User, ct));
                        var parsed = PptxArticleWriter.ParseSlides(r3, 10);
                        if (parsed.Count == 10) trDeck = parsed; // else keep the deterministic 10-slide deck
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio offline — used built-in 10-slide deck.");
                }
                PptxArticleWriter.Write(trDeck, Path.Combine(OutputDirectory, "transfer_learning_analysis.pptx"), trFont);

                Result?.Invoke(TransferContent.Scorecard(trDash, trTopDomain) + "\n\n" + trSkill);
                Status?.Invoke($"Saved your Transfer Learning package to {OutputDirectory}.");
                Completed?.Invoke(Path.Combine(OutputDirectory, "transfer_learning_report.docx"));
                return;
            }

            if (turing)
            {
                Directory.CreateDirectory(OutputDirectory);
                var tgWords = accumulator.Words;
                string tgSeed = accumulator.Seed();
                int tgDistinct = tgWords.Distinct(StringComparer.OrdinalIgnoreCase).Count();

                // recorded_eeg.csv (+ timestamped snapshot for continuous recordings) + translated_eeg.txt
                File.WriteAllText(Path.Combine(OutputDirectory, "recorded_eeg.csv"), NlpContent.RecordedEegCsv(tgWords));
                var tgStamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                File.WriteAllText(Path.Combine(OutputDirectory, $"recorded_eeg_{tgStamp}.csv"), NlpContent.RecordedEegCsv(tgWords));
                string tgStatsHdr =
                    "# Translated EEG\n" +
                    $"recorded_seconds: {seconds}\n" +
                    $"words: {accumulator.Count}\n" +
                    $"distinct_words: {tgDistinct}\n" +
                    $"signal_quality: {(tgWords.Count > 0 ? "good" : "no signal")}\n" +
                    $"avg_attention: {avgAtt:0}\n" +
                    $"avg_meditation: {avgMed:0}\n" +
                    $"dominant_band: {domKey}\n\n";
                File.WriteAllText(Path.Combine(OutputDirectory, "translated_eeg.txt"), NlpContent.TranslatedText(tgStatsHdr, tgWords));

                // deterministic Turing pipeline (offline-safe)
                var tgDash = TuringProfile.Dashboard(avgAtt, avgMed, bandReadings, tgWords);
                double tgHuman = TuringProfile.HumanLikeness(avgAtt, bandReadings, tgWords);
                double tgMachine = TuringProfile.MachineLikeness(bandReadings, tgWords);
                string tgVerdict = TuringContent.Verdict(tgHuman, tgMachine);

                File.WriteAllText(Path.Combine(OutputDirectory, "human_thought_profile.csv"), TuringProfile.HumanThoughtProfileCsv(avgAtt, avgMed, bandReadings, tgWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "human_likeness.csv"), TuringProfile.HumanLikenessCsv(avgAtt, bandReadings, tgWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "machine_likeness.csv"), TuringProfile.MachineLikenessCsv(bandReadings, tgWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "judge_results.csv"), TuringProfile.JudgeResultsCsv(avgAtt, bandReadings, tgWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "cognitive_comparison.csv"), TuringProfile.CognitiveComparisonCsv(avgAtt, bandReadings, tgWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "reasoning_comparison.csv"), TuringProfile.ReasoningComparisonCsv(avgAtt, bandReadings, tgWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "creativity_challenge.csv"), TuringProfile.CreativityChallengeCsv(bandReadings, tgWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "knowledge_comparison.csv"), TuringProfile.KnowledgeComparisonCsv(avgAtt, bandReadings, tgWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "turing_probability.csv"), TuringProfile.TuringProbabilityCsv(avgAtt, bandReadings, tgWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "multi_model_results.csv"), TuringProfile.MultiModelResultsCsv(avgAtt, bandReadings, tgWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "leaderboard.csv"), TuringProfile.LeaderboardCsv(avgAtt, avgMed, bandReadings, tgWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "artificial_brain_comparison.csv"), TuringProfile.ArtificialBrainComparisonCsv(avgAtt, bandReadings, tgWords));

                // historical analysis
                TuringProfile.AppendHistory(Path.Combine(OutputDirectory, "turing_history.csv"), avgAtt, avgMed, bandReadings, tgWords);

                string tgFont = string.IsNullOrWhiteSpace(_config.Font) ? "Verdana" : _config.Font;

                // LM 1 — two narrative sections (artificial thoughts, human-vs-AI chat)
                string tgArtificial = TuringContent.DefaultArtificialThoughts(tgWords);
                string tgChat = TuringContent.DefaultHumanVsAiChat(tgWords);
                try
                {
                    Status?.Invoke("Generating AI thoughts with LM Studio…");
                    using var c1 = new LmStudioClient(_config.LmStudioUrl);
                    string m1 = _config.Model;
                    if (string.IsNullOrWhiteSpace(m1)) m1 = await c1.GetFirstModelAsync(ct) ?? "";
                    if (!string.IsNullOrWhiteSpace(m1))
                    {
                        var p1 = TuringPromptBuilder.BuildNarratives(tgSeed, avgAtt, avgMed, domKey, profile);
                        var r1 = RewritePromptBuilder.CleanReply(await c1.CompleteAsync(m1, p1.System, p1.User, ct));
                        var at = NlpContent.ExtractTextSection(r1, "# ARTIFICIAL THOUGHTS", "# HUMAN VS AI CHAT");
                        var ch = NlpContent.ExtractTextSection(r1, "# HUMAN VS AI CHAT", null);
                        if (!string.IsNullOrWhiteSpace(at)) tgArtificial = at;
                        if (!string.IsNullOrWhiteSpace(ch)) tgChat = ch;
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio offline — used built-in AI thoughts.");
                }
                File.WriteAllText(Path.Combine(OutputDirectory, "artificial_thoughts.txt"), tgArtificial);
                File.WriteAllText(Path.Combine(OutputDirectory, "human_vs_ai_chat.txt"), tgChat);

                // LM 2 — research report -> .docx
                string tgReport = TuringContent.DefaultReportMarkdown(tgDash, tgWords, avgAtt, avgMed, domKey, tgVerdict);
                try
                {
                    Status?.Invoke("Writing the Turing test report with LM Studio…");
                    using var c2 = new LmStudioClient(_config.LmStudioUrl);
                    string m2 = _config.Model;
                    if (string.IsNullOrWhiteSpace(m2)) m2 = await c2.GetFirstModelAsync(ct) ?? "";
                    if (!string.IsNullOrWhiteSpace(m2))
                    {
                        var p2 = TuringPromptBuilder.BuildReport(tgSeed, seconds, tgVerdict, avgAtt, avgMed, domKey, profile);
                        var r2 = RewritePromptBuilder.CleanReply(await c2.CompleteAsync(m2, p2.System, p2.User, ct));
                        if (!string.IsNullOrWhiteSpace(r2)) tgReport = r2;
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio offline — used built-in report.");
                }
                DocxArticleWriter.Write(tgReport, Path.Combine(OutputDirectory, "turing_test_report.docx"), tgFont);

                // LM 3 — 10-slide deck (accept only a full 10-slide LM deck)
                var tgDeck = TuringContent.DefaultDeck(tgDash, tgWords, avgAtt, avgMed, domKey, tgVerdict);
                try
                {
                    Status?.Invoke("Building the 10-slide deck with LM Studio…");
                    using var c3 = new LmStudioClient(_config.LmStudioUrl);
                    string m3 = _config.Model;
                    if (string.IsNullOrWhiteSpace(m3)) m3 = await c3.GetFirstModelAsync(ct) ?? "";
                    if (!string.IsNullOrWhiteSpace(m3))
                    {
                        var p3 = TuringPromptBuilder.BuildSlides(tgVerdict, avgAtt, avgMed, domKey);
                        var r3 = RewritePromptBuilder.CleanReply(await c3.CompleteAsync(m3, p3.System, p3.User, ct));
                        var parsed = PptxArticleWriter.ParseSlides(r3, 10);
                        if (parsed.Count == 10) tgDeck = parsed; // else keep the deterministic 10-slide deck
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio offline — used built-in 10-slide deck.");
                }
                PptxArticleWriter.Write(tgDeck, Path.Combine(OutputDirectory, "turing_test_analysis.pptx"), tgFont);

                Result?.Invoke(TuringContent.Scorecard(tgDash, tgVerdict) + "\n\n" + tgChat);
                Status?.Invoke($"Saved your Turing Test package to {OutputDirectory}.");
                Completed?.Invoke(Path.Combine(OutputDirectory, "turing_test_report.docx"));
                return;
            }

            if (unsup)
            {
                Directory.CreateDirectory(OutputDirectory);
                var usWords = accumulator.Words;
                string usSeed = accumulator.Seed();
                int usDistinct = usWords.Distinct(StringComparer.OrdinalIgnoreCase).Count();
                var dataDir = Path.Combine(AppContext.BaseDirectory, "data");

                int usPrior = 0;
                try { usPrior = Directory.GetFiles(OutputDirectory, "recorded_eeg_*.csv").Length; } catch { /* none */ }

                // recorded_eeg.csv (+ timestamped snapshot for multi-session) + translated_eeg.txt
                File.WriteAllText(Path.Combine(OutputDirectory, "recorded_eeg.csv"), NlpContent.RecordedEegCsv(usWords));
                var usStamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                File.WriteAllText(Path.Combine(OutputDirectory, $"recorded_eeg_{usStamp}.csv"), NlpContent.RecordedEegCsv(usWords));
                string usStatsHdr =
                    "# Translated EEG\n" +
                    $"recorded_seconds: {seconds}\n" +
                    $"words: {accumulator.Count}\n" +
                    $"distinct_words: {usDistinct}\n" +
                    $"signal_quality: {(usWords.Count > 0 ? "good" : "no signal")}\n" +
                    $"avg_attention: {avgAtt:0}\n" +
                    $"avg_meditation: {avgMed:0}\n" +
                    $"dominant_band: {domKey}\n\n";
                File.WriteAllText(Path.Combine(OutputDirectory, "translated_eeg.txt"), NlpContent.TranslatedText(usStatsHdr, usWords));

                // deterministic unsupervised pipeline (offline-safe; ML methods simulated)
                var usTopicObj = UnsupTopics.LoadFromDir(dataDir);
                var usTopics = usTopicObj.Detect(usWords);
                int usActiveTopics = usTopics.Count(t => t.Count > 0);
                var usDash = UnsupProfile.Dashboard(avgAtt, avgMed, bandReadings, usWords, usActiveTopics);
                string usDomTopic = usTopics.Count > 0 ? usTopics[0].Topic : "General";

                // feature extraction
                File.WriteAllText(Path.Combine(OutputDirectory, "signal_features.csv"), UnsupProfile.SignalFeaturesCsv(avgAtt, avgMed, bandReadings, usWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "text_features.csv"), UnsupContent.TextFeaturesCsv(usWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "embedding_vectors.csv"), UnsupContent.EmbeddingVectorsCsv(usWords));
                // clustering
                File.WriteAllText(Path.Combine(OutputDirectory, "cluster_assignments.csv"), UnsupContent.ClusterAssignmentsCsv(usWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "cluster_statistics.csv"), UnsupContent.ClusterStatisticsCsv(usWords, usTopicObj));
                // dimensionality reduction
                File.WriteAllText(Path.Combine(OutputDirectory, "pca_projection.csv"), UnsupContent.ProjectionCsv(usWords, "pca"));
                File.WriteAllText(Path.Combine(OutputDirectory, "tsne_projection.csv"), UnsupContent.ProjectionCsv(usWords, "tsne"));
                File.WriteAllText(Path.Combine(OutputDirectory, "umap_projection.csv"), UnsupContent.ProjectionCsv(usWords, "umap"));
                // topic discovery
                File.WriteAllText(Path.Combine(OutputDirectory, "latent_topics.csv"), UnsupContent.LatentTopicsCsv(usTopics));
                File.WriteAllText(Path.Combine(OutputDirectory, "topic_keywords.csv"), UnsupContent.TopicKeywordsCsv(usTopics, usWords, usTopicObj));
                File.WriteAllText(Path.Combine(OutputDirectory, "topic_distributions.csv"), UnsupContent.TopicDistributionsCsv(usWords, usTopicObj));
                // similarity
                File.WriteAllText(Path.Combine(OutputDirectory, "similarity_matrix.csv"), UnsupContent.SimilarityMatrixCsv(usWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "nearest_neighbors.csv"), UnsupContent.NearestNeighborsCsv(usWords));
                // anomaly
                File.WriteAllText(Path.Combine(OutputDirectory, "anomaly_scores.csv"), UnsupContent.AnomalyScoresCsv(usWords));
                // association
                File.WriteAllText(Path.Combine(OutputDirectory, "association_rules.csv"), UnsupContent.AssociationRulesCsv(usWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "concept_network.csv"), UnsupContent.ConceptNetworkCsv(usWords));
                // archetypes
                File.WriteAllText(Path.Combine(OutputDirectory, "cognitive_archetypes.csv"), UnsupProfile.CognitiveArchetypesCsv(avgAtt, avgMed, bandReadings, usWords));
                // multi-user network
                File.WriteAllText(Path.Combine(OutputDirectory, "user_clusters.csv"), UnsupContent.UserClustersCsv(usPrior));
                File.WriteAllText(Path.Combine(OutputDirectory, "community_network.csv"), UnsupContent.CommunityNetworkCsv(usPrior));
                File.WriteAllText(Path.Combine(OutputDirectory, "emergent_roles.csv"), UnsupContent.EmergentRolesCsv(usPrior));

                // long-term learning database
                UnsupProfile.AppendHistory(Path.Combine(OutputDirectory, "unsupervised_history.csv"), avgAtt, avgMed, bandReadings, usWords, usActiveTopics, usDomTopic);

                string usFont = string.IsNullOrWhiteSpace(_config.Font) ? "Verdana" : _config.Font;

                // LM 1 — two narrative sections (emergent behaviors, rare patterns)
                string usEmergent = UnsupContent.DefaultEmergentBehaviors(usTopics, usWords);
                string usRare = UnsupContent.DefaultRarePatterns(usWords);
                try
                {
                    Status?.Invoke("Discovering structure with LM Studio…");
                    using var c1 = new LmStudioClient(_config.LmStudioUrl);
                    string m1 = _config.Model;
                    if (string.IsNullOrWhiteSpace(m1)) m1 = await c1.GetFirstModelAsync(ct) ?? "";
                    if (!string.IsNullOrWhiteSpace(m1))
                    {
                        var p1 = UnsupPromptBuilder.BuildNarratives(usSeed, usTopics, avgAtt, avgMed, domKey, profile);
                        var r1 = RewritePromptBuilder.CleanReply(await c1.CompleteAsync(m1, p1.System, p1.User, ct));
                        var eb = NlpContent.ExtractTextSection(r1, "# EMERGENT BEHAVIORS", "# RARE PATTERNS");
                        var rp = NlpContent.ExtractTextSection(r1, "# RARE PATTERNS", null);
                        if (!string.IsNullOrWhiteSpace(eb)) usEmergent = eb;
                        if (!string.IsNullOrWhiteSpace(rp)) usRare = rp;
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio offline — used built-in emergent behaviors.");
                }
                File.WriteAllText(Path.Combine(OutputDirectory, "emergent_behaviors.txt"), usEmergent);
                File.WriteAllText(Path.Combine(OutputDirectory, "rare_patterns.txt"), usRare);

                // LM 2 — research report -> .docx
                string usReport = UnsupContent.DefaultReportMarkdown(usDash, usTopics, usWords, avgAtt, avgMed, domKey);
                try
                {
                    Status?.Invoke("Writing the unsupervised-learning report with LM Studio…");
                    using var c2 = new LmStudioClient(_config.LmStudioUrl);
                    string m2 = _config.Model;
                    if (string.IsNullOrWhiteSpace(m2)) m2 = await c2.GetFirstModelAsync(ct) ?? "";
                    if (!string.IsNullOrWhiteSpace(m2))
                    {
                        var p2 = UnsupPromptBuilder.BuildReport(usSeed, seconds, usTopics, avgAtt, avgMed, domKey, profile);
                        var r2 = RewritePromptBuilder.CleanReply(await c2.CompleteAsync(m2, p2.System, p2.User, ct));
                        if (!string.IsNullOrWhiteSpace(r2)) usReport = r2;
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio offline — used built-in report.");
                }
                DocxArticleWriter.Write(usReport, Path.Combine(OutputDirectory, "unsupervised_learning_report.docx"), usFont);

                // LM 3 — 10-slide deck (accept only a full 10-slide LM deck)
                var usDeck = UnsupContent.DefaultDeck(usDash, usTopics, usWords, avgAtt, avgMed, domKey);
                try
                {
                    Status?.Invoke("Building the 10-slide deck with LM Studio…");
                    using var c3 = new LmStudioClient(_config.LmStudioUrl);
                    string m3 = _config.Model;
                    if (string.IsNullOrWhiteSpace(m3)) m3 = await c3.GetFirstModelAsync(ct) ?? "";
                    if (!string.IsNullOrWhiteSpace(m3))
                    {
                        var p3 = UnsupPromptBuilder.BuildSlides(usTopics, avgAtt, avgMed, domKey);
                        var r3 = RewritePromptBuilder.CleanReply(await c3.CompleteAsync(m3, p3.System, p3.User, ct));
                        var parsed = PptxArticleWriter.ParseSlides(r3, 10);
                        if (parsed.Count == 10) usDeck = parsed; // else keep the deterministic 10-slide deck
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio offline — used built-in 10-slide deck.");
                }
                PptxArticleWriter.Write(usDeck, Path.Combine(OutputDirectory, "unsupervised_learning_analysis.pptx"), usFont);

                Result?.Invoke(UnsupContent.Scorecard(usDash, 6, usDomTopic) + "\n\n" + usEmergent);
                Status?.Invoke($"Saved your Unsupervised Learning package to {OutputDirectory}.");
                Completed?.Invoke(Path.Combine(OutputDirectory, "unsupervised_learning_report.docx"));
                return;
            }

            if (vrworld)
            {
                Directory.CreateDirectory(OutputDirectory);
                var vrWords = accumulator.Words;
                string vrSeed = accumulator.Seed();
                int vrDistinct = vrWords.Distinct(StringComparer.OrdinalIgnoreCase).Count();

                // recorded_eeg.csv (+ timestamped snapshot for multiple sessions) + translated_eeg.txt
                File.WriteAllText(Path.Combine(OutputDirectory, "recorded_eeg.csv"), NlpContent.RecordedEegCsv(vrWords));
                var vrStamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                File.WriteAllText(Path.Combine(OutputDirectory, $"recorded_eeg_{vrStamp}.csv"), NlpContent.RecordedEegCsv(vrWords));
                string vrStatsHdr =
                    "# Translated EEG\n" +
                    $"recorded_seconds: {seconds}\n" +
                    $"words: {accumulator.Count}\n" +
                    $"distinct_words: {vrDistinct}\n" +
                    $"signal_quality: {(vrWords.Count > 0 ? "good" : "no signal")}\n" +
                    $"avg_attention: {avgAtt:0}\n" +
                    $"avg_meditation: {avgMed:0}\n" +
                    $"dominant_band: {domKey}\n\n";
                File.WriteAllText(Path.Combine(OutputDirectory, "translated_eeg.txt"), NlpContent.TranslatedText(vrStatsHdr, vrWords));

                // deterministic structures (offline-safe)
                var vrDash = VrProfile.Dashboard(avgAtt, avgMed, bandReadings, vrWords);
                string vrTheme = VrContent.Theme(vrWords);
                File.WriteAllText(Path.Combine(OutputDirectory, "virtual_characters.csv"), VrContent.VirtualCharactersCsv(vrWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "vr_controls.csv"), VrContent.VrControlsCsv(vrWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "ai_companions.csv"), VrContent.AiCompanionsCsv(vrWords));
                VrProfile.AppendHistory(Path.Combine(OutputDirectory, "vr_history.csv"), avgAtt, avgMed, bandReadings, vrWords, vrTheme);

                string vrFont = string.IsNullOrWhiteSpace(_config.Font) ? "Verdana" : _config.Font;

                // LM 1 — world bundle (virtual worlds, world blueprints, VR experiences)
                string vrWorlds = VrContent.DefaultVirtualWorlds(vrWords);
                string vrBlueprints = VrContent.DefaultWorldBlueprints(vrWords);
                string vrExperiences = VrContent.DefaultVrExperiences(vrWords);
                try
                {
                    Status?.Invoke("Generating virtual worlds with LM Studio…");
                    using var c1 = new LmStudioClient(_config.LmStudioUrl);
                    string m1 = _config.Model;
                    if (string.IsNullOrWhiteSpace(m1)) m1 = await c1.GetFirstModelAsync(ct) ?? "";
                    if (!string.IsNullOrWhiteSpace(m1))
                    {
                        var p1 = VrPromptBuilder.BuildWorlds(vrSeed, avgAtt, avgMed, domKey, profile);
                        var r1 = RewritePromptBuilder.CleanReply(await c1.CompleteAsync(m1, p1.System, p1.User, ct));
                        var vw = NlpContent.ExtractTextSection(r1, "# VIRTUAL WORLDS", "# WORLD BLUEPRINTS");
                        var wb = NlpContent.ExtractTextSection(r1, "# WORLD BLUEPRINTS", "# VR EXPERIENCES");
                        var ex = NlpContent.ExtractTextSection(r1, "# VR EXPERIENCES", null);
                        if (!string.IsNullOrWhiteSpace(vw)) vrWorlds = vw;
                        if (!string.IsNullOrWhiteSpace(wb)) vrBlueprints = wb;
                        if (!string.IsNullOrWhiteSpace(ex)) vrExperiences = ex;
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio offline — used built-in virtual worlds.");
                }
                File.WriteAllText(Path.Combine(OutputDirectory, "virtual_worlds.txt"), vrWorlds);
                File.WriteAllText(Path.Combine(OutputDirectory, "world_blueprints.md"), vrBlueprints);
                File.WriteAllText(Path.Combine(OutputDirectory, "vr_experiences.txt"), vrExperiences);

                // LM 2 — creative bundle (architecture prompts, VR story, training worlds, Meta Quest prompts)
                string vrArch = VrContent.DefaultArchitecturePrompts(vrWords);
                string vrStory = VrContent.DefaultVrStory(vrWords);
                string vrTraining = VrContent.DefaultTrainingWorlds(vrWords);
                string vrMeta = VrContent.DefaultMetaquestPrompts(vrWords);
                try
                {
                    Status?.Invoke("Designing prompts & story with LM Studio…");
                    using var c2 = new LmStudioClient(_config.LmStudioUrl);
                    string m2 = _config.Model;
                    if (string.IsNullOrWhiteSpace(m2)) m2 = await c2.GetFirstModelAsync(ct) ?? "";
                    if (!string.IsNullOrWhiteSpace(m2))
                    {
                        var p2 = VrPromptBuilder.BuildCreative(vrSeed, avgAtt, avgMed, domKey, profile);
                        var r2 = RewritePromptBuilder.CleanReply(await c2.CompleteAsync(m2, p2.System, p2.User, ct));
                        var ap = NlpContent.ExtractTextSection(r2, "# ARCHITECTURE PROMPTS", "# VR STORY");
                        var st = NlpContent.ExtractTextSection(r2, "# VR STORY", "# TRAINING WORLDS");
                        var tw = NlpContent.ExtractTextSection(r2, "# TRAINING WORLDS", "# METAQUEST PROMPTS");
                        var mq = NlpContent.ExtractTextSection(r2, "# METAQUEST PROMPTS", null);
                        if (!string.IsNullOrWhiteSpace(ap)) vrArch = ap;
                        if (!string.IsNullOrWhiteSpace(st)) vrStory = st;
                        if (!string.IsNullOrWhiteSpace(tw)) vrTraining = tw;
                        if (!string.IsNullOrWhiteSpace(mq)) vrMeta = mq;
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio offline — used built-in prompts & story.");
                }
                File.WriteAllText(Path.Combine(OutputDirectory, "architecture_prompts.txt"), vrArch);
                File.WriteAllText(Path.Combine(OutputDirectory, "vr_story.md"), vrStory);
                File.WriteAllText(Path.Combine(OutputDirectory, "training_worlds.txt"), vrTraining);
                File.WriteAllText(Path.Combine(OutputDirectory, "metaquest_prompts.txt"), vrMeta);

                // LM 3 — innovation bundle (innovation simulations, emotional worlds, shared worlds)
                string vrInnov = VrContent.DefaultInnovationSimulations(vrWords);
                string vrEmotional = VrContent.DefaultEmotionalWorlds(vrWords, avgAtt, avgMed);
                string vrShared = VrContent.DefaultSharedWorlds(vrWords);
                try
                {
                    Status?.Invoke("Simulating innovation worlds with LM Studio…");
                    using var c3 = new LmStudioClient(_config.LmStudioUrl);
                    string m3 = _config.Model;
                    if (string.IsNullOrWhiteSpace(m3)) m3 = await c3.GetFirstModelAsync(ct) ?? "";
                    if (!string.IsNullOrWhiteSpace(m3))
                    {
                        var p3 = VrPromptBuilder.BuildInnovation(vrSeed, avgAtt, avgMed, domKey, profile);
                        var r3 = RewritePromptBuilder.CleanReply(await c3.CompleteAsync(m3, p3.System, p3.User, ct));
                        var inv = NlpContent.ExtractTextSection(r3, "# INNOVATION SIMULATIONS", "# EMOTIONAL WORLDS");
                        var em = NlpContent.ExtractTextSection(r3, "# EMOTIONAL WORLDS", "# SHARED WORLDS");
                        var sh = NlpContent.ExtractTextSection(r3, "# SHARED WORLDS", null);
                        if (!string.IsNullOrWhiteSpace(inv)) vrInnov = inv;
                        if (!string.IsNullOrWhiteSpace(em)) vrEmotional = em;
                        if (!string.IsNullOrWhiteSpace(sh)) vrShared = sh;
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio offline — used built-in innovation worlds.");
                }
                File.WriteAllText(Path.Combine(OutputDirectory, "innovation_simulations.txt"), vrInnov);
                File.WriteAllText(Path.Combine(OutputDirectory, "emotional_worlds.txt"), vrEmotional);
                File.WriteAllText(Path.Combine(OutputDirectory, "shared_virtual_worlds.txt"), vrShared);

                // LM 4 — research report -> .docx
                string vrReport = VrContent.DefaultReportMarkdown(vrDash, vrWords, avgAtt, avgMed, domKey, vrTheme);
                try
                {
                    Status?.Invoke("Writing the VR analysis report with LM Studio…");
                    using var c4 = new LmStudioClient(_config.LmStudioUrl);
                    string m4 = _config.Model;
                    if (string.IsNullOrWhiteSpace(m4)) m4 = await c4.GetFirstModelAsync(ct) ?? "";
                    if (!string.IsNullOrWhiteSpace(m4))
                    {
                        var p4 = VrPromptBuilder.BuildReport(vrSeed, seconds, vrTheme, avgAtt, avgMed, domKey, profile);
                        var r4 = RewritePromptBuilder.CleanReply(await c4.CompleteAsync(m4, p4.System, p4.User, ct));
                        if (!string.IsNullOrWhiteSpace(r4)) vrReport = r4;
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio offline — used built-in report.");
                }
                DocxArticleWriter.Write(vrReport, Path.Combine(OutputDirectory, "virtual_reality_analysis.docx"), vrFont);

                // LM 5 — 10-slide deck (accept only a full 10-slide LM deck)
                var vrDeck = VrContent.DefaultDeck(vrDash, vrWords, avgAtt, avgMed, domKey, vrTheme);
                try
                {
                    Status?.Invoke("Building the 10-slide deck with LM Studio…");
                    using var c5 = new LmStudioClient(_config.LmStudioUrl);
                    string m5 = _config.Model;
                    if (string.IsNullOrWhiteSpace(m5)) m5 = await c5.GetFirstModelAsync(ct) ?? "";
                    if (!string.IsNullOrWhiteSpace(m5))
                    {
                        var p5 = VrPromptBuilder.BuildSlides(vrTheme, avgAtt, avgMed, domKey);
                        var r5 = RewritePromptBuilder.CleanReply(await c5.CompleteAsync(m5, p5.System, p5.User, ct));
                        var parsed = PptxArticleWriter.ParseSlides(r5, 10);
                        if (parsed.Count == 10) vrDeck = parsed; // else keep the deterministic 10-slide deck
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio offline — used built-in 10-slide deck.");
                }
                PptxArticleWriter.Write(vrDeck, Path.Combine(OutputDirectory, "virtual_reality_analysis.pptx"), vrFont);

                Result?.Invoke(VrContent.Scorecard(vrDash, vrTheme) + "\n\n" + vrWorlds);
                Status?.Invoke($"Saved your Virtual Reality package to {OutputDirectory}.");
                Completed?.Invoke(Path.Combine(OutputDirectory, "virtual_reality_analysis.docx"));
                return;
            }

            if (voice)
            {
                Directory.CreateDirectory(OutputDirectory);
                var voWords = accumulator.Words;
                string voSeed = accumulator.Seed();
                int voDistinct = voWords.Distinct(StringComparer.OrdinalIgnoreCase).Count();
                var dataDir = Path.Combine(AppContext.BaseDirectory, "data");

                // recorded_voice.wav (silent placeholder — no microphone in this environment) + recorded_eeg.csv (+ snapshot) + translated_eeg.txt
                VoiceContent.WriteSilentWav(Path.Combine(OutputDirectory, "recorded_voice.wav"));
                File.WriteAllText(Path.Combine(OutputDirectory, "recorded_eeg.csv"), NlpContent.RecordedEegCsv(voWords));
                var voStamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                File.WriteAllText(Path.Combine(OutputDirectory, $"recorded_eeg_{voStamp}.csv"), NlpContent.RecordedEegCsv(voWords));
                string voStatsHdr =
                    "# Translated EEG\n" +
                    $"recorded_seconds: {seconds}\n" +
                    $"words: {accumulator.Count}\n" +
                    $"distinct_words: {voDistinct}\n" +
                    $"signal_quality: {(voWords.Count > 0 ? "good" : "no signal")}\n" +
                    $"avg_attention: {avgAtt:0}\n" +
                    $"avg_meditation: {avgMed:0}\n" +
                    $"dominant_band: {domKey}\n\n";
                File.WriteAllText(Path.Combine(OutputDirectory, "translated_eeg.txt"), NlpContent.TranslatedText(voStatsHdr, voWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "transcribed_speech.txt"), VoiceContent.TranscribedSpeech(voWords, seconds));

                // deterministic voice analysis (offline-safe)
                var voTopicObj = VoiceTopics.LoadFromDir(dataDir);
                var voTopics = voTopicObj.Detect(voWords);
                int voActiveTopics = voTopics.Count(t => t.Count > 0);
                string voTopTopic = voTopics.Count > 0 ? voTopics[0].Topic : "General";
                string voStyle = VoiceProfile.DominantStyle(avgAtt, bandReadings, voWords);
                var voDash = VoiceProfile.Dashboard(avgAtt, avgMed, bandReadings, voWords, seconds, voActiveTopics);

                File.WriteAllText(Path.Combine(OutputDirectory, "speech_statistics.csv"), VoiceProfile.SpeechStatisticsCsv(avgAtt, avgMed, bandReadings, voWords, seconds));
                File.WriteAllText(Path.Combine(OutputDirectory, "voice_features.csv"), VoiceProfile.VoiceFeaturesCsv(avgAtt, avgMed, bandReadings, voWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "communication_style.csv"), VoiceProfile.CommunicationStyleCsv(avgAtt, bandReadings, voWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "voice_topics.csv"), VoiceContent.VoiceTopicsCsv(voTopics));
                File.WriteAllText(Path.Combine(OutputDirectory, "voice_sentiment.csv"), VoiceProfile.SentimentCsv(avgAtt, avgMed, bandReadings, voWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "speaker_profile.csv"), VoiceProfile.SpeakerProfileCsv(avgAtt, avgMed, bandReadings, voWords, voTopTopic));
                File.WriteAllText(Path.Combine(OutputDirectory, "voice_eeg_correlation.csv"), VoiceProfile.VoiceEegCorrelationCsv(avgAtt, bandReadings, voWords, voActiveTopics));
                File.WriteAllText(Path.Combine(OutputDirectory, "keywords.csv"), VoiceContent.KeywordsCsv(voWords, voTopicObj));
                File.WriteAllText(Path.Combine(OutputDirectory, "voice_knowledge_graph.md"), VoiceContent.KnowledgeGraphMd(voWords, voTopics));
                File.WriteAllText(Path.Combine(OutputDirectory, "presentation_evaluation.csv"), VoiceProfile.PresentationEvaluationCsv(avgAtt, avgMed, bandReadings, voWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "speaker_comparison.csv"), VoiceProfile.SpeakerComparisonCsv(avgAtt, bandReadings, voWords));

                // voice memory database
                VoiceProfile.AppendHistory(Path.Combine(OutputDirectory, "voice_history.csv"), avgAtt, avgMed, bandReadings, voWords, seconds, voActiveTopics);

                string voFont = string.IsNullOrWhiteSpace(_config.Font) ? "Verdana" : _config.Font;

                // LM 1 — three narrative sections (chat log, learning analysis, communication forecast)
                string voChat = VoiceContent.DefaultChatLog(voWords);
                string voLearning = VoiceContent.DefaultLearningAnalysis(voTopics, voWords);
                string voForecast = VoiceContent.DefaultForecast(voTopics);
                try
                {
                    Status?.Invoke("Analyzing voice with LM Studio…");
                    using var c1 = new LmStudioClient(_config.LmStudioUrl);
                    string m1 = _config.Model;
                    if (string.IsNullOrWhiteSpace(m1)) m1 = await c1.GetFirstModelAsync(ct) ?? "";
                    if (!string.IsNullOrWhiteSpace(m1))
                    {
                        var p1 = VoicePromptBuilder.BuildNarratives(voSeed, voTopics, avgAtt, avgMed, domKey, profile);
                        var r1 = RewritePromptBuilder.CleanReply(await c1.CompleteAsync(m1, p1.System, p1.User, ct));
                        var cl = NlpContent.ExtractTextSection(r1, "# VOICE CHAT LOG", "# LEARNING ANALYSIS");
                        var la = NlpContent.ExtractTextSection(r1, "# LEARNING ANALYSIS", "# COMMUNICATION FORECAST");
                        var fc = NlpContent.ExtractTextSection(r1, "# COMMUNICATION FORECAST", null);
                        if (!string.IsNullOrWhiteSpace(cl)) voChat = cl;
                        if (!string.IsNullOrWhiteSpace(la)) voLearning = la;
                        if (!string.IsNullOrWhiteSpace(fc)) voForecast = fc;
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio offline — used built-in voice narratives.");
                }
                File.WriteAllText(Path.Combine(OutputDirectory, "voice_chat_log.txt"), voChat);
                File.WriteAllText(Path.Combine(OutputDirectory, "learning_analysis.txt"), voLearning);
                File.WriteAllText(Path.Combine(OutputDirectory, "communication_forecast.txt"), voForecast);

                // LM 2 — research report -> .docx
                string voReport = VoiceContent.DefaultReportMarkdown(voDash, voTopics, voWords, avgAtt, avgMed, domKey, voStyle);
                try
                {
                    Status?.Invoke("Writing the voice-recognition report with LM Studio…");
                    using var c2 = new LmStudioClient(_config.LmStudioUrl);
                    string m2 = _config.Model;
                    if (string.IsNullOrWhiteSpace(m2)) m2 = await c2.GetFirstModelAsync(ct) ?? "";
                    if (!string.IsNullOrWhiteSpace(m2))
                    {
                        var p2 = VoicePromptBuilder.BuildReport(voSeed, seconds, voTopics, voStyle, avgAtt, avgMed, domKey, profile);
                        var r2 = RewritePromptBuilder.CleanReply(await c2.CompleteAsync(m2, p2.System, p2.User, ct));
                        if (!string.IsNullOrWhiteSpace(r2)) voReport = r2;
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio offline — used built-in report.");
                }
                DocxArticleWriter.Write(voReport, Path.Combine(OutputDirectory, "voice_recognition_report.docx"), voFont);

                // LM 3 — 10-slide deck (accept only a full 10-slide LM deck)
                var voDeck = VoiceContent.DefaultDeck(voDash, voTopics, voWords, avgAtt, avgMed, domKey, voStyle);
                try
                {
                    Status?.Invoke("Building the 10-slide deck with LM Studio…");
                    using var c3 = new LmStudioClient(_config.LmStudioUrl);
                    string m3 = _config.Model;
                    if (string.IsNullOrWhiteSpace(m3)) m3 = await c3.GetFirstModelAsync(ct) ?? "";
                    if (!string.IsNullOrWhiteSpace(m3))
                    {
                        var p3 = VoicePromptBuilder.BuildSlides(voTopics, voStyle, avgAtt, avgMed, domKey);
                        var r3 = RewritePromptBuilder.CleanReply(await c3.CompleteAsync(m3, p3.System, p3.User, ct));
                        var parsed = PptxArticleWriter.ParseSlides(r3, 10);
                        if (parsed.Count == 10) voDeck = parsed; // else keep the deterministic 10-slide deck
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio offline — used built-in 10-slide deck.");
                }
                PptxArticleWriter.Write(voDeck, Path.Combine(OutputDirectory, "voice_recognition_analysis.pptx"), voFont);

                Result?.Invoke(VoiceContent.Scorecard(voDash, voTopTopic, voStyle) + "\n\n" + voLearning);
                Status?.Invoke($"Saved your Voice Recognition package to {OutputDirectory}.");
                Completed?.Invoke(Path.Combine(OutputDirectory, "voice_recognition_report.docx"));
                return;
            }

            if (weakai)
            {
                Directory.CreateDirectory(OutputDirectory);
                var waWords = accumulator.Words;
                string waSeed = accumulator.Seed();
                int waDistinct = waWords.Distinct(StringComparer.OrdinalIgnoreCase).Count();
                var dataDir = Path.Combine(AppContext.BaseDirectory, "data");

                // recorded_eeg.csv (+ snapshot for continuous recording) + translated_eeg.txt
                File.WriteAllText(Path.Combine(OutputDirectory, "recorded_eeg.csv"), NlpContent.RecordedEegCsv(waWords));
                var waStamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                File.WriteAllText(Path.Combine(OutputDirectory, $"recorded_eeg_{waStamp}.csv"), NlpContent.RecordedEegCsv(waWords));
                string waStatsHdr =
                    "# Translated EEG\n" +
                    $"recorded_seconds: {seconds}\n" +
                    $"words: {accumulator.Count}\n" +
                    $"distinct_words: {waDistinct}\n" +
                    $"signal_quality: {(waWords.Count > 0 ? "good" : "no signal")}\n" +
                    $"avg_attention: {avgAtt:0}\n" +
                    $"avg_meditation: {avgMed:0}\n" +
                    $"dominant_band: {domKey}\n\n";
                File.WriteAllText(Path.Combine(OutputDirectory, "translated_eeg.txt"), NlpContent.TranslatedText(waStatsHdr, waWords));

                // deterministic Weak AI pipeline (offline-safe)
                var waDomainObj = WeakAiDomains.LoadFromDir(dataDir);
                var waDomains = waDomainObj.Detect(waWords);
                int waActive = waDomains.Count(d => d.Count > 0);
                double waTopPercent = waDomains.Count > 0 ? waDomains[0].Percent : 0;
                string waTopDomain = waDomains.Count > 0 ? waDomains[0].Domain : "General";
                string waCognitive = WeakAiProfile.DominantCognitive(avgAtt, avgMed, bandReadings, waWords);
                var waDash = WeakAiProfile.Dashboard(avgAtt, avgMed, bandReadings, waWords, waTopPercent, waActive);
                int waRecCount = WeakAiContent.TaskCount(waWords);

                File.WriteAllText(Path.Combine(OutputDirectory, "detected_domains.csv"), WeakAiContent.DetectedDomainsCsv(waDomains));
                File.WriteAllText(Path.Combine(OutputDirectory, "cognitive_classification.csv"), WeakAiProfile.CognitiveClassificationCsv(avgAtt, avgMed, bandReadings, waWords));
                File.WriteAllText(Path.Combine(OutputDirectory, "task_recommendations.csv"), WeakAiContent.TaskRecommendationsCsv(waWords, waDomains));
                File.WriteAllText(Path.Combine(OutputDirectory, "knowledge_extraction.csv"), WeakAiContent.KnowledgeExtractionCsv(waWords, waDomainObj));
                File.WriteAllText(Path.Combine(OutputDirectory, "productivity_analysis.csv"), WeakAiProfile.ProductivityAnalysisCsv(avgAtt, avgMed, bandReadings, waWords, waRecCount));
                File.WriteAllText(Path.Combine(OutputDirectory, "weak_ai_metrics.csv"), WeakAiProfile.MetricsCsv(avgAtt, avgMed, bandReadings, waWords, waTopPercent));

                // multi-session learning
                WeakAiProfile.AppendHistory(Path.Combine(OutputDirectory, "weak_ai_history.csv"), avgAtt, avgMed, bandReadings, waWords, waTopPercent, waActive, waTopDomain);

                string waFont = string.IsNullOrWhiteSpace(_config.Font) ? "Verdana" : _config.Font;

                // LM 1 — specialized assistant bundle (research, engineering, programming, learning)
                string waResearch = WeakAiContent.DefaultResearchAssistant(waDomains, waWords);
                string waEngineering = WeakAiContent.DefaultEngineeringAssistant(waDomains, waWords);
                string waProgramming = WeakAiContent.DefaultProgrammingAssistant(waWords);
                string waLearning = WeakAiContent.DefaultLearningAssistant(waDomains, waWords);
                try
                {
                    Status?.Invoke("Running specialized assistants with LM Studio…");
                    using var c1 = new LmStudioClient(_config.LmStudioUrl);
                    string m1 = _config.Model;
                    if (string.IsNullOrWhiteSpace(m1)) m1 = await c1.GetFirstModelAsync(ct) ?? "";
                    if (!string.IsNullOrWhiteSpace(m1))
                    {
                        var p1 = WeakAiPromptBuilder.BuildAssistants(waSeed, waDomains, avgAtt, avgMed, domKey, profile);
                        var r1 = RewritePromptBuilder.CleanReply(await c1.CompleteAsync(m1, p1.System, p1.User, ct));
                        var ra = NlpContent.ExtractTextSection(r1, "# RESEARCH ASSISTANT", "# ENGINEERING ASSISTANT");
                        var ea = NlpContent.ExtractTextSection(r1, "# ENGINEERING ASSISTANT", "# PROGRAMMING ASSISTANT");
                        var pa = NlpContent.ExtractTextSection(r1, "# PROGRAMMING ASSISTANT", "# LEARNING ASSISTANT");
                        var la = NlpContent.ExtractTextSection(r1, "# LEARNING ASSISTANT", null);
                        if (!string.IsNullOrWhiteSpace(ra)) waResearch = ra;
                        if (!string.IsNullOrWhiteSpace(ea)) waEngineering = ea;
                        if (!string.IsNullOrWhiteSpace(pa)) waProgramming = pa;
                        if (!string.IsNullOrWhiteSpace(la)) waLearning = la;
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio offline — used built-in assistants.");
                }
                File.WriteAllText(Path.Combine(OutputDirectory, "research_assistant_report.txt"), waResearch);
                File.WriteAllText(Path.Combine(OutputDirectory, "engineering_assistant_report.txt"), waEngineering);
                File.WriteAllText(Path.Combine(OutputDirectory, "programming_assistant_report.txt"), waProgramming);
                File.WriteAllText(Path.Combine(OutputDirectory, "learning_assistant_report.txt"), waLearning);

                // LM 2 — support bundle (chat log, decision support, future predictions, expert profiles)
                string waChat = WeakAiContent.DefaultChatLog(waWords);
                string waDecision = WeakAiContent.DefaultDecisionSupport(waWords);
                string waFuture = WeakAiContent.DefaultFuturePredictions(waDomains);
                string waExperts = WeakAiContent.DefaultExpertProfiles(waDomains, waWords);
                try
                {
                    Status?.Invoke("Supporting decisions with LM Studio…");
                    using var c2 = new LmStudioClient(_config.LmStudioUrl);
                    string m2 = _config.Model;
                    if (string.IsNullOrWhiteSpace(m2)) m2 = await c2.GetFirstModelAsync(ct) ?? "";
                    if (!string.IsNullOrWhiteSpace(m2))
                    {
                        var p2 = WeakAiPromptBuilder.BuildSupport(waSeed, waDomains, avgAtt, avgMed, domKey, profile);
                        var r2 = RewritePromptBuilder.CleanReply(await c2.CompleteAsync(m2, p2.System, p2.User, ct));
                        var cl = NlpContent.ExtractTextSection(r2, "# CHAT LOG", "# DECISION SUPPORT");
                        var ds = NlpContent.ExtractTextSection(r2, "# DECISION SUPPORT", "# FUTURE PREDICTIONS");
                        var fp = NlpContent.ExtractTextSection(r2, "# FUTURE PREDICTIONS", "# EXPERT PROFILES");
                        var ep = NlpContent.ExtractTextSection(r2, "# EXPERT PROFILES", null);
                        if (!string.IsNullOrWhiteSpace(cl)) waChat = cl;
                        if (!string.IsNullOrWhiteSpace(ds)) waDecision = ds;
                        if (!string.IsNullOrWhiteSpace(fp)) waFuture = fp;
                        if (!string.IsNullOrWhiteSpace(ep)) waExperts = ep;
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio offline — used built-in support outputs.");
                }
                File.WriteAllText(Path.Combine(OutputDirectory, "weak_ai_chatlog.txt"), waChat);
                File.WriteAllText(Path.Combine(OutputDirectory, "decision_support_report.txt"), waDecision);
                File.WriteAllText(Path.Combine(OutputDirectory, "future_task_predictions.txt"), waFuture);
                File.WriteAllText(Path.Combine(OutputDirectory, "expert_profiles.md"), waExperts);

                // LM 3 — domain knowledge report -> .docx
                string waReport = WeakAiContent.DefaultReportMarkdown(waDash, waDomains, waWords, avgAtt, avgMed, domKey, waCognitive);
                try
                {
                    Status?.Invoke("Writing the domain knowledge report with LM Studio…");
                    using var c3 = new LmStudioClient(_config.LmStudioUrl);
                    string m3 = _config.Model;
                    if (string.IsNullOrWhiteSpace(m3)) m3 = await c3.GetFirstModelAsync(ct) ?? "";
                    if (!string.IsNullOrWhiteSpace(m3))
                    {
                        var p3 = WeakAiPromptBuilder.BuildReport(waSeed, seconds, waDomains, waCognitive, avgAtt, avgMed, domKey, profile);
                        var r3 = RewritePromptBuilder.CleanReply(await c3.CompleteAsync(m3, p3.System, p3.User, ct));
                        if (!string.IsNullOrWhiteSpace(r3)) waReport = r3;
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio offline — used built-in report.");
                }
                DocxArticleWriter.Write(waReport, Path.Combine(OutputDirectory, "domain_knowledge_report.docx"), waFont);

                // LM 4 — 10-slide deck (accept only a full 10-slide LM deck)
                var waDeck = WeakAiContent.DefaultDeck(waDash, waDomains, waWords, avgAtt, avgMed, domKey, waCognitive);
                try
                {
                    Status?.Invoke("Building the 10-slide deck with LM Studio…");
                    using var c4 = new LmStudioClient(_config.LmStudioUrl);
                    string m4 = _config.Model;
                    if (string.IsNullOrWhiteSpace(m4)) m4 = await c4.GetFirstModelAsync(ct) ?? "";
                    if (!string.IsNullOrWhiteSpace(m4))
                    {
                        var p4 = WeakAiPromptBuilder.BuildSlides(waDomains, waCognitive, avgAtt, avgMed, domKey);
                        var r4 = RewritePromptBuilder.CleanReply(await c4.CompleteAsync(m4, p4.System, p4.User, ct));
                        var parsed = PptxArticleWriter.ParseSlides(r4, 10);
                        if (parsed.Count == 10) waDeck = parsed; // else keep the deterministic 10-slide deck
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Status?.Invoke("LM Studio offline — used built-in 10-slide deck.");
                }
                PptxArticleWriter.Write(waDeck, Path.Combine(OutputDirectory, "weak_ai_analysis.pptx"), waFont);

                Result?.Invoke(WeakAiContent.Scorecard(waDash, waTopDomain, waCognitive, waRecCount) + "\n\n" + waResearch);
                Status?.Invoke($"Saved your Weak AI package to {OutputDirectory}.");
                Completed?.Invoke(Path.Combine(OutputDirectory, "domain_knowledge_report.docx"));
                return;
            }

            // ===== LM Studio kinds: rewrite (.txt), article (.docx), advice (.pdf), python (.py) =====
            var prompt = rewrite ? RewritePromptBuilder.Build(accumulator.Seed(), seconds)
                       : article ? ArticlePromptBuilder.Build(accumulator.Seed(), seconds)
                       : advice ? SelfImprovementPromptBuilder.Build(accumulator.Seed(), seconds, avgAtt, avgMed, domKey, profile)
                       : aitheory ? AiTheoryPromptBuilder.Build(accumulator.Seed(), seconds, avgAtt, avgMed, domKey, profile)
                       : algorithm ? AlgorithmPromptBuilder.Build(accumulator.Seed(), seconds, avgAtt, avgMed, domKey, profile)
                       : slides ? SlidesPromptBuilder.Build(accumulator.Seed(), seconds, avgAtt, avgMed, domKey, profile)
                       : analysis ? AnalysisPromptBuilder.Build(accumulator.Seed(), seconds, avgAtt, avgMed, bandReadings, domKey, profile)
                       : artificial ? ArtificialBrainPromptBuilder.Build(accumulator.Seed(), seconds, avgAtt, avgMed, bandReadings, domKey, profile)
                       : questvr ? QuestVrPromptBuilder.Build(accumulator.Seed(), seconds, avgAtt, avgMed, domKey, profile)
                       : chip ? ChipPromptBuilder.Build(accumulator.Seed(), seconds, avgAtt, avgMed, domKey, profile)
                       : cloudeval ? CloudComputingPromptBuilder.Build(accumulator.Seed(), seconds, avgAtt, avgMed, domKey, profile)
                       : mltheory ? MachineLearningPromptBuilder.Build(accumulator.Seed(), seconds, avgAtt, avgMed, domKey, profile)
                       : deeplearning ? DeepLearningPromptBuilder.Build(accumulator.Seed(), seconds, avgAtt, avgMed, domKey, profile)
                       : ecommerce ? EcommercePromptBuilder.Build(accumulator.Seed(), seconds, avgAtt, avgMed, domKey, profile)
                       : emergent ? EmergentBehaviorPromptBuilder.Build(accumulator.Seed(), seconds, avgAtt, avgMed, domKey, profile)
                       : emotional ? EmotionalAiPromptBuilder.Build(accumulator.Seed(), seconds, avgAtt, avgMed, domKey, profile)
                       : gear ? GearPromptBuilder.Build(accumulator.Seed(), seconds, avgAtt, avgMed, domKey, profile)
                       : blender ? BlenderPromptBuilder.Build(accumulator.Seed(), seconds, avgAtt, avgMed, domKey, profile, _config.Subject, _config.Tool)
                       : ArmyPromptBuilder.Build(accumulator.Seed(), seconds, _config.PromptSkew);
            promptPath = Path.Combine(OutputDirectory, $"{baseName}.prompt.txt");
            File.WriteAllText(promptPath, prompt.System + "\n\n---\n\n" + prompt.User);

            Status?.Invoke("Contacting LM Studio…");
            using var client = new LmStudioClient(_config.LmStudioUrl);

            string model = _config.Model;
            if (string.IsNullOrWhiteSpace(model))
            {
                model = await client.GetFirstModelAsync(ct)
                        ?? throw new InvalidOperationException("LM Studio reported no loaded model.");
            }
            Status?.Invoke($"{(rewrite ? "Rewriting" : article ? "Writing article" : advice ? "Writing advice" : algorithm ? "Writing algorithm" : slides ? "Building slides" : analysis ? "Analyzing" : artificial ? "Studying machine vs human" : aitheory ? "Formulating theory" : blender ? "Crafting prompt" : questvr ? "Designing XR apps" : chip ? "Designing chip" : cloudeval ? "Evaluating cloud reasoning" : deeplearning ? "Designing deep learning model" : ecommerce ? "Designing eCommerce store" : emergent ? "Detecting behavior skew" : emotional ? "Assessing emotions" : gear ? "Engineering the concept" : mltheory ? "Building ML knowledge" : "Generating")} with '{model}'…");

            var reply = await client.CompleteAsync(model, prompt.System, prompt.User, ct);

            // Study (.md) or prompt (.txt) kinds: save + preview.
            if (analysis || artificial || blender || questvr || chip || cloudeval || deeplearning || ecommerce || emergent || mltheory)
            {
                var text = RewritePromptBuilder.CleanReply(reply);
                if (string.IsNullOrWhiteSpace(text))
                    throw new InvalidOperationException("LM Studio returned no content.");

                var textPath = Path.Combine(OutputDirectory, $"{baseName}.{(blender || questvr || chip ? "txt" : "md")}");
                File.WriteAllText(textPath, text);

                Result?.Invoke(text);
                Status?.Invoke($"Saved {Path.GetFileName(textPath)}");
                Completed?.Invoke(textPath);
                return;
            }

            // Slide deck: parse the model's SLIDE blocks and render a .pptx.
            if (slides)
            {
                var raw = RewritePromptBuilder.CleanReply(reply);
                if (string.IsNullOrWhiteSpace(raw))
                    throw new InvalidOperationException("LM Studio returned no content.");

                File.WriteAllText(Path.Combine(OutputDirectory, $"{baseName}.md"), raw);
                var deck = PptxArticleWriter.ParseSlides(raw, 6);

                var pptxPath = Path.Combine(OutputDirectory, $"{baseName}.pptx");
                PptxArticleWriter.Write(deck, pptxPath, _config.Font);

                Result?.Invoke(raw);
                Status?.Invoke($"Saved {Path.GetFileName(pptxPath)} ({deck.Count} slides, {_config.Font})");
                Completed?.Invoke(pptxPath);
                return;
            }

            // Document kinds: render the model's Markdown to a formatted .docx or .pdf.
            if (article || advice || aitheory || emotional || gear)
            {
                var markdown = RewritePromptBuilder.CleanReply(reply); // strip stray ``` fences
                if (string.IsNullOrWhiteSpace(markdown))
                    throw new InvalidOperationException("LM Studio returned no content.");

                File.WriteAllText(Path.Combine(OutputDirectory, $"{baseName}.md"), markdown);

                string docPath;
                if (advice || aitheory || emotional || gear)
                {
                    docPath = Path.Combine(OutputDirectory, $"{baseName}.pdf");
                    PdfArticleWriter.Write(markdown, docPath, _config.Font);
                }
                else
                {
                    docPath = Path.Combine(OutputDirectory, $"{baseName}.docx");
                    DocxArticleWriter.Write(markdown, docPath, _config.Font);
                }

                Result?.Invoke(markdown);
                Status?.Invoke($"Saved {Path.GetFileName(docPath)} ({_config.Font})");
                Completed?.Invoke(docPath);
                return;
            }

            var content = rewrite ? RewritePromptBuilder.CleanReply(reply) : ArmyPromptBuilder.ExtractPython(reply);
            if (string.IsNullOrWhiteSpace(content))
                throw new InvalidOperationException("LM Studio returned no content.");

            var outPath = Path.Combine(OutputDirectory, $"{baseName}.{(rewrite ? "txt" : "py")}");
            File.WriteAllText(outPath, content);

            if (rewrite || algorithm) Result?.Invoke(content);
            Status?.Invoke($"Saved {Path.GetFileName(outPath)} ({content.Split('\n').Length} lines)");
            Completed?.Invoke(outPath);
        }
        catch (OperationCanceledException)
        {
            Status?.Invoke("Cancelled.");
        }
        catch (Exception ex)
        {
            var note = promptPath is not null
                ? $"LM Studio not reachable — prompt saved to {Path.GetFileName(promptPath)}. {ex.Message}"
                : $"Failed before prompt was built. {ex.Message}";
            Status?.Invoke(note);
            Failed?.Invoke(note);
        }
        finally
        {
            source.Event -= OnEvent;
            if (useProcessor) await source.DisconnectAsync(); // tear down the artificial brain
            IsRunning = false;
        }
    }
}
