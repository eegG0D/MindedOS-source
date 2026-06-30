using System.Text.Json.Serialization;

namespace MindedOS.Engine;

/// <summary>A trigger/condition clause: signal `op` value (e.g. attention &gt; 70).</summary>
public sealed class Clause
{
    [JsonPropertyName("signal")] public string Signal { get; set; } = "attention";
    [JsonPropertyName("op")] public string Op { get; set; } = ">";
    [JsonPropertyName("value")] public double Value { get; set; }
}

/// <summary>One control on a sub-program form, bound to a computer action.</summary>
public sealed class ControlSpec
{
    [JsonPropertyName("type")] public string Type { get; set; } = "Button";
    [JsonPropertyName("label")] public string Label { get; set; } = "";
    [JsonPropertyName("action")] public string? Action { get; set; }

    /// <summary>Fires the action on a rising edge of this signal threshold.</summary>
    [JsonPropertyName("trigger")] public Clause? Trigger { get; set; }

    /// <summary>Optional gate; the trigger only fires while this holds.</summary>
    [JsonPropertyName("condition")] public Clause? Condition { get; set; }

    // Optional explicit layout (AHK-style); auto-laid-out when omitted.
    [JsonPropertyName("x")] public double? X { get; set; }
    [JsonPropertyName("y")] public double? Y { get; set; }
    [JsonPropertyName("w")] public double? W { get; set; }
    [JsonPropertyName("h")] public double? H { get; set; }

    /// <summary>Items for list/combo controls.</summary>
    [JsonPropertyName("items")] public List<string>? Items { get; set; }
}

/// <summary>A sub-program ("app") loaded from a JSON file.</summary>
public sealed class SubProgram
{
    [JsonPropertyName("name")] public string Name { get; set; } = "Untitled";

    /// <summary>Segoe Fluent Icons glyph hex code, e.g. "E7C5".</summary>
    [JsonPropertyName("icon")] public string Icon { get; set; } = "E700";

    [JsonPropertyName("description")] public string? Description { get; set; }

    /// <summary>Mental profiles this program auto-launches for.</summary>
    [JsonPropertyName("profiles")] public List<string> Profiles { get; set; } = new();

    /// <summary>Optional AHK template name to seed the layout (e.g. "Program100").</summary>
    [JsonPropertyName("template")] public string? Template { get; set; }

    [JsonPropertyName("fullscreen")] public bool Fullscreen { get; set; }

    [JsonPropertyName("controls")] public List<ControlSpec> Controls { get; set; } = new();

    /// <summary>
    /// EEG-matched move sequences attached to this program (file names under the
    /// sequences/ folder, with or without .json). Two or more may be listed.
    /// </summary>
    [JsonPropertyName("sequences")] public List<string> Sequences { get; set; } = new();

    /// <summary>
    /// Optional: turns this program into an AI Application that accumulates the
    /// EEG-to-words stream, builds a prompt, and asks LM Studio to generate a
    /// Python program (saved as a .py file). See <see cref="AiAppConfig"/>.
    /// </summary>
    [JsonPropertyName("aiApp")] public AiAppConfig? AiApp { get; set; }

    /// <summary>
    /// Optional: turns this program into an "Artificial Life" comparison — record
    /// the CPU's EEG and the user's EEG to CSVs and compute how artificial the
    /// user's brain looks. See <see cref="ArtLifeConfig"/>.
    /// </summary>
    [JsonPropertyName("artLife")] public ArtLifeConfig? ArtLife { get; set; }

    /// <summary>
    /// Optional: turns this program into an Artificial Neural Network leaderboard —
    /// scan a folder of per-user EEG CSVs, record your own, and rank the network.
    /// </summary>
    [JsonPropertyName("network")] public NetworkConfig? Network { get; set; }

    /// <summary>
    /// Optional: turns this program into an Artificial Noosphere — scan hundreds of
    /// EEG CSVs into a matrix, find the most advanced and assign company roles.
    /// </summary>
    [JsonPropertyName("noosphere")] public NetworkConfig? Noosphere { get; set; }

    /// <summary>
    /// Optional: turns this program into the EEG autonomous-vehicle driving game.
    /// </summary>
    [JsonPropertyName("vehicle")] public VehicleConfig? Vehicle { get; set; }

    /// <summary>
    /// Optional: turns this program into the Big Data profession analyzer — record
    /// long EEG files and classify them into top professions via profession_map.csv.
    /// </summary>
    [JsonPropertyName("bigData")] public BigDataConfig? BigData { get; set; }

    /// <summary>
    /// Optional: turns this program into Black Box Learning — study a subject for a
    /// fixed window, record the EEG, and compute learning stats from the CSV.
    /// </summary>
    [JsonPropertyName("blackBox")] public BlackBoxConfig? BlackBox { get; set; }

    /// <summary>
    /// Optional: turns this program into the Brain-Machine-Interface 4-direction
    /// mini-game (live EEG or CSV playback; auto-records when no CSV exists).
    /// </summary>
    [JsonPropertyName("bmi")] public BmiConfig? Bmi { get; set; }

    /// <summary>
    /// Optional: turns this program into the EEG chatbot — it reads your decoded
    /// EEG words and improvises a reply to each via LM Studio.
    /// </summary>
    [JsonPropertyName("chatbot")] public ChatbotConfig? Chatbot { get; set; }

    /// <summary>
    /// Optional: turns this program into the Choice Descriptor — record EEG-mapped
    /// choices for a window, then process the CSV with LM Studio.
    /// </summary>
    [JsonPropertyName("choices")] public ChoiceConfig? Choices { get; set; }

    /// <summary>
    /// Optional: turns this program into Data Ingestion — profile a huge CSV and
    /// summarize it into one sentence with LM Studio.
    /// </summary>
    [JsonPropertyName("dataIngest")] public DataIngestConfig? DataIngest { get; set; }

    /// <summary>
    /// Optional: turns this program into Decision — LM Studio reads PDFs in a folder
    /// and the EEG is rewritten into a decision about each file.
    /// </summary>
    [JsonPropertyName("decision")] public DecisionConfig? Decision { get; set; }

    /// <summary>
    /// Optional: turns this program into the Intelligent Assistant — match live EEG
    /// readings to eeg_map_assist.csv and offer three services per try.
    /// </summary>
    [JsonPropertyName("assist")] public AssistConfig? Assist { get; set; }

    /// <summary>
    /// Optional: turns this program into the IoT robot controller — stream live
    /// EEG-mapped commands out a COM port to a robot or robotic arm.
    /// </summary>
    [JsonPropertyName("iot")] public IotConfig? Iot { get; set; }

    /// <summary>
    /// Optional: turns this program into the Limited Memory Machine — record EEG as
    /// a command memory, assign commands (LM Studio or by hand), and reload it.
    /// </summary>
    [JsonPropertyName("memory")] public MemoryConfig? Memory { get; set; }

    /// <summary>Source file path, filled in by the loader.</summary>
    [JsonIgnore] public string SourcePath { get; set; } = "";
}

/// <summary>Configuration for the Limited Memory Machine.</summary>
public sealed class MemoryConfig
{
    /// <summary>Seconds of EEG recorded into the memory (default 180 = 3 minutes).</summary>
    [JsonPropertyName("recordSeconds")] public int RecordSeconds { get; set; } = 180;

    /// <summary>Folder for recorded memory CSVs; empty = Documents\mindedOS\memory.</summary>
    [JsonPropertyName("outputDir")] public string OutputDir { get; set; } = "";

    [JsonPropertyName("lmStudioUrl")] public string LmStudioUrl { get; set; } = "http://localhost:1234";
    [JsonPropertyName("model")] public string Model { get; set; } = "";
}

/// <summary>Configuration for the Intelligent Assistant.</summary>
public sealed class AssistConfig
{
    /// <summary>Assistant map CSV under the data folder (eeg,word,service).</summary>
    [JsonPropertyName("mapFile")] public string MapFile { get; set; } = "eeg_map_assist.csv";
}

/// <summary>Configuration for the IoT robot controller.</summary>
public sealed class IotConfig
{
    /// <summary>EEG→command CSV under the data folder (raw_eeg,word,command).</summary>
    [JsonPropertyName("mapFile")] public string MapFile { get; set; } = "eeg_map_iot.csv";

    /// <summary>Serial baud rate for the robot's COM/Bluetooth port.</summary>
    [JsonPropertyName("baud")] public int Baud { get; set; } = 9600;

    /// <summary>How often (ms) the live EEG is sampled and a command is streamed.</summary>
    [JsonPropertyName("intervalMs")] public int IntervalMs { get; set; } = 600;
}

/// <summary>Configuration for the EEG document-decision program.</summary>
public sealed class DecisionConfig
{
    /// <summary>Folder of PDF files; empty = Documents\mindedOS\decisions.</summary>
    [JsonPropertyName("outputDir")] public string OutputDir { get; set; } = "";

    /// <summary>Characters of each PDF to read.</summary>
    [JsonPropertyName("maxChars")] public int MaxChars { get; set; } = 4000;

    [JsonPropertyName("lmStudioUrl")] public string LmStudioUrl { get; set; } = "http://localhost:1234";
    [JsonPropertyName("model")] public string Model { get; set; } = "";
}

/// <summary>Configuration for the Data Ingestion summarizer.</summary>
public sealed class DataIngestConfig
{
    /// <summary>Folder of datasets; empty = Documents\mindedOS\data_ingestion.</summary>
    [JsonPropertyName("outputDir")] public string OutputDir { get; set; } = "";

    /// <summary>Rows sampled for type/stat inference (all rows are still counted).</summary>
    [JsonPropertyName("sampleRows")] public int SampleRows { get; set; } = 10000;

    [JsonPropertyName("lmStudioUrl")] public string LmStudioUrl { get; set; } = "http://localhost:1234";
    [JsonPropertyName("model")] public string Model { get; set; } = "";
}

/// <summary>Configuration for the Choice Descriptor.</summary>
public sealed class ChoiceConfig
{
    /// <summary>Recording length (default 600 = 10 minutes).</summary>
    [JsonPropertyName("recordSeconds")] public int RecordSeconds { get; set; } = 600;

    /// <summary>Folder for recorded choice CSVs; empty = Documents\mindedOS\choices.</summary>
    [JsonPropertyName("outputDir")] public string OutputDir { get; set; } = "";

    /// <summary>Choice-map CSV under the data folder.</summary>
    [JsonPropertyName("mapFile")] public string MapFile { get; set; } = "eeg_map_choices.csv";

    [JsonPropertyName("lmStudioUrl")] public string LmStudioUrl { get; set; } = "http://localhost:1234";
    [JsonPropertyName("model")] public string Model { get; set; } = "";
}

/// <summary>Configuration for the EEG chatbot.</summary>
public sealed class ChatbotConfig
{
    /// <summary>How often (seconds) to sample a brain word and reply.</summary>
    [JsonPropertyName("intervalSeconds")] public int IntervalSeconds { get; set; } = 5;

    [JsonPropertyName("lmStudioUrl")] public string LmStudioUrl { get; set; } = "http://localhost:1234";
    [JsonPropertyName("model")] public string Model { get; set; } = "";
}

/// <summary>Configuration for the Brain-Machine-Interface mini-game.</summary>
public sealed class BmiConfig
{
    /// <summary>Seconds per auto-recorded segment (default 300 = 5 minutes).</summary>
    [JsonPropertyName("recordSeconds")] public int RecordSeconds { get; set; } = 300;

    /// <summary>Folder for recorded / played-back CSVs; empty = Documents\mindedOS\bmi.</summary>
    [JsonPropertyName("outputDir")] public string OutputDir { get; set; } = "";

    /// <summary>Control-map CSV under the data folder.</summary>
    [JsonPropertyName("mapFile")] public string MapFile { get; set; } = "eeg_map_bmi.csv";

    [JsonPropertyName("lmStudioUrl")] public string LmStudioUrl { get; set; } = "http://localhost:1234";
    [JsonPropertyName("model")] public string Model { get; set; } = "";
}

/// <summary>Configuration for the Black Box Learning study analyzer.</summary>
public sealed class BlackBoxConfig
{
    /// <summary>Study-session length (default 1920 = 32 minutes).</summary>
    [JsonPropertyName("studySeconds")] public int StudySeconds { get; set; } = 1920;

    /// <summary>Folder for recorded study CSVs; empty = Documents\mindedOS\black_box.</summary>
    [JsonPropertyName("outputDir")] public string OutputDir { get; set; } = "";

    [JsonPropertyName("lmStudioUrl")] public string LmStudioUrl { get; set; } = "http://localhost:1234";
    [JsonPropertyName("model")] public string Model { get; set; } = "";
}

/// <summary>Configuration for the Big Data profession analyzer.</summary>
public sealed class BigDataConfig
{
    /// <summary>Length of a Big Data recording (default 14400 = 4 hours).</summary>
    [JsonPropertyName("recordSeconds")] public int RecordSeconds { get; set; } = 14400;

    /// <summary>Folder for recorded Big Data CSVs; empty = Documents\mindedOS\big_data.</summary>
    [JsonPropertyName("outputDir")] public string OutputDir { get; set; } = "";

    /// <summary>Profession map CSV under the data folder.</summary>
    [JsonPropertyName("mapFile")] public string MapFile { get; set; } = "profession_map.csv";

    [JsonPropertyName("lmStudioUrl")] public string LmStudioUrl { get; set; } = "http://localhost:1234";
    [JsonPropertyName("model")] public string Model { get; set; } = "";
}

/// <summary>Configuration for the EEG autonomous-vehicle driving game.</summary>
public sealed class VehicleConfig
{
    /// <summary>Move-mapping CSV under the data folder.</summary>
    [JsonPropertyName("mapFile")] public string MapFile { get; set; } = "eeg_map_vehicle.csv";

    /// <summary>Folder for the recorded raw-EEG CSV; empty = Documents\mindedOS\vehicle.</summary>
    [JsonPropertyName("outputDir")] public string OutputDir { get; set; } = "";

    [JsonPropertyName("lmStudioUrl")] public string LmStudioUrl { get; set; } = "http://localhost:1234";
    [JsonPropertyName("model")] public string Model { get; set; } = "";
}

/// <summary>Configuration for the Artificial Life CPU-vs-human EEG comparison.</summary>
public sealed class ArtLifeConfig
{
    /// <summary>Seconds to record each stream (3 min default).</summary>
    [JsonPropertyName("recordSeconds")] public int RecordSeconds { get; set; } = 180;

    /// <summary>Output folder; empty = Documents\mindedOS\artificial_life.</summary>
    [JsonPropertyName("outputDir")] public string OutputDir { get; set; } = "";

    [JsonPropertyName("cpuFile")] public string CpuFile { get; set; } = "cpu_eeg.csv";
    [JsonPropertyName("userFile")] public string UserFile { get; set; } = "user_eeg.csv";
}

/// <summary>Configuration for the Artificial Neural Network brain leaderboard.</summary>
public sealed class NetworkConfig
{
    /// <summary>Seconds to record your own EEG (3 min default).</summary>
    [JsonPropertyName("recordSeconds")] public int RecordSeconds { get; set; } = 180;

    /// <summary>Folder holding one CSV per user; empty = Documents\mindedOS\neural_network.</summary>
    [JsonPropertyName("outputDir")] public string OutputDir { get; set; } = "";
}

/// <summary>Configuration for an AI-Application program (LM Studio code generation).</summary>
public sealed class AiAppConfig
{
    /// <summary>LM Studio OpenAI-compatible base URL (model is auto-detected from it).</summary>
    [JsonPropertyName("lmStudioUrl")] public string LmStudioUrl { get; set; } = "http://localhost:1234";

    /// <summary>Optional fixed model id; empty = auto-detect the loaded model.</summary>
    [JsonPropertyName("model")] public string Model { get; set; } = "";

    /// <summary>Seconds of EEG-to-words to accumulate before generating (3 min default).</summary>
    [JsonPropertyName("accumulateSeconds")] public int AccumulateSeconds { get; set; } = 180;

    /// <summary>Theme bias for the generated artifact. "army" by default.</summary>
    [JsonPropertyName("promptSkew")] public string PromptSkew { get; set; } = "army";

    /// <summary>
    /// What to generate: "python" (a .py app, default) or "famitracker" (a .txt
    /// song importable into FamiTracker). Controls the prompt, extraction and file
    /// extension.
    /// </summary>
    [JsonPropertyName("kind")] public string Kind { get; set; } = "python";

    /// <summary>Output folder; empty = Documents\mindedOS\generated_py.</summary>
    [JsonPropertyName("outputDir")] public string OutputDir { get; set; } = "";

    /// <summary>Label shown next to the editable address box in the AI panel.</summary>
    [JsonPropertyName("addressLabel")] public string AddressLabel { get; set; } = "Output folder / address";

    /// <summary>Font for generated documents (e.g. the .docx article). Default Verdana.</summary>
    [JsonPropertyName("font")] public string Font { get; set; } = "Verdana";

    /// <summary>Subject for a generated prompt (e.g. "Architecture"). Used by the blender kind.</summary>
    [JsonPropertyName("subject")] public string Subject { get; set; } = "Architecture";

    /// <summary>Target tool the generated prompt is for (e.g. "Blender").</summary>
    [JsonPropertyName("tool")] public string Tool { get; set; } = "Blender";

    /// <summary>
    /// Optional CSV under the data folder used by mapping kinds (e.g. the
    /// "healthcare" kind reads the EEG→drug catalog from here).
    /// </summary>
    [JsonPropertyName("mapFile")] public string MapFile { get; set; } = "";
}
