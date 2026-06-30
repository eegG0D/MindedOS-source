using System.Text;

namespace MindedOS.Engine;

/// <summary>One Group Policy rule in the hardening baseline.</summary>
public sealed record GpoRule(string Category, string Policy, string Value, string Why);

/// <summary>A generated, coherent set of GPO rules with a codename and posture.</summary>
public sealed record GpoBaselineResult(string Codename, bool Strict, IReadOnlyList<GpoRule> Rules);

/// <summary>
/// Deterministically builds a coherent set of 35 Windows Group Policy rules that
/// work together as one hardening baseline (no conflicts, defense in depth). The
/// EEG seeds the codename and the posture (focused → strict thresholds); LM Studio
/// only writes the elaboration — so the 35 rules are always complete and valid.
/// </summary>
public static class GpoBaseline
{
    private static readonly GpoRule[] Base =
    {
        new("Password Policy", "Minimum password length", "12 characters", "Resist brute-force and guessing."),
        new("Password Policy", "Enforce password history", "24 passwords remembered", "Prevent reuse of old credentials."),
        new("Password Policy", "Maximum password age", "365 days", "Bound the lifetime of any credential."),
        new("Password Policy", "Minimum password age", "1 day", "Stop rapid cycling that defeats history."),
        new("Password Policy", "Password must meet complexity requirements", "Enabled", "Raise password entropy."),
        new("Password Policy", "Store passwords using reversible encryption", "Disabled", "Never keep plaintext-equivalent secrets."),
        new("Account Lockout", "Account lockout threshold", "5 invalid attempts", "Throttle online password guessing."),
        new("Account Lockout", "Account lockout duration", "15 minutes", "Slow attackers without locking forever."),
        new("Account Lockout", "Reset lockout counter after", "15 minutes", "Balance security and usability."),
        new("Security Options", "Accounts: Guest account status", "Disabled", "Remove anonymous local access."),
        new("Security Options", "Accounts: Limit local account use of blank passwords to console only", "Enabled", "Block remote blank-password logon."),
        new("Security Options", "Interactive logon: Machine inactivity limit", "900 seconds", "Auto-lock idle, unattended sessions."),
        new("Security Options", "Interactive logon: Do not require CTRL+ALT+DEL", "Disabled (require it)", "Defeat credential-spoofing overlays."),
        new("Security Options", "User Account Control: Run all administrators in Admin Approval Mode", "Enabled", "Force elevation for admin actions."),
        new("Security Options", "UAC: Behavior of the elevation prompt for administrators", "Prompt for consent on the secure desktop", "Resist UAC bypass and spoofing."),
        new("Security Options", "Network security: LAN Manager authentication level", "Send NTLMv2 only; refuse LM & NTLM", "Kill weak legacy auth."),
        new("Security Options", "Network security: Do not store LAN Manager hash on next change", "Enabled", "Eliminate easily cracked LM hashes."),
        new("Security Options", "WDigest Authentication (UseLogonCredential)", "Disabled", "Stop plaintext credentials in memory."),
        new("Network", "Microsoft network client: Digitally sign communications (always)", "Enabled", "SMB signing to prevent tampering/relay."),
        new("Network", "Microsoft network server: Digitally sign communications (always)", "Enabled", "SMB signing on the server side too."),
        new("Network", "Network access: Do not allow anonymous enumeration of SAM accounts and shares", "Enabled", "Deny recon of users and shares."),
        new("Network", "Configure SMBv1 client/server", "Disabled (removed)", "Eliminate the legacy SMBv1 attack surface."),
        new("Network", "Turn off multicast name resolution (LLMNR)", "Enabled", "Prevent LLMNR/NBNS credential capture."),
        new("Audit Policy", "Audit Logon", "Success and Failure", "Record interactive/remote logons."),
        new("Audit Policy", "Audit Credential Validation", "Success and Failure", "Detect password-spray and auth abuse."),
        new("Audit Policy", "Audit Account Management", "Success and Failure", "Track user/group changes."),
        new("Audit Policy", "Audit Policy Change", "Success and Failure", "Catch tampering with policy itself."),
        new("Audit Policy", "Audit Sensitive Privilege Use", "Success and Failure", "Flag use of powerful rights."),
        new("Firewall", "Windows Defender Firewall: All profiles", "On", "Host firewall on Domain/Private/Public."),
        new("Firewall", "Inbound / Outbound default action", "Inbound: Block, Outbound: Allow", "Default-deny inbound, defense in depth."),
        new("Firewall", "Log dropped packets", "Enabled", "Forensic visibility into blocked traffic."),
        new("Defender", "Microsoft Defender Antivirus: Real-time protection", "Enabled", "Continuous malware protection."),
        new("Defender", "Cloud-delivered protection (MAPS)", "Enabled (Advanced)", "Faster detection of new threats."),
        new("Defender", "Attack Surface Reduction rules", "Enabled (Block)", "Block common malware behaviors."),
        new("Administrative Templates", "Turn off Autoplay; Turn on PowerShell Script Block Logging", "Autoplay Off (all drives); Script Block Logging Enabled", "Cut autorun infection + log malicious scripts."),
    };

    public static GpoBaselineResult Build(string wordSeed, double avgAttention)
    {
        bool strict = avgAttention > 55;
        var rules = new List<GpoRule>(Base);

        if (strict)
        {
            rules[0] = rules[0] with { Value = "14 characters" };
            rules[6] = rules[6] with { Value = "3 invalid attempts" };
            rules[11] = rules[11] with { Value = "300 seconds" };
        }

        string codename = Codename(wordSeed);
        return new GpoBaselineResult(codename, strict, rules);
    }

    private static string Codename(string wordSeed)
    {
        var word = (wordSeed ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault(w => w.Length >= 4);
        if (string.IsNullOrEmpty(word)) return "Bastion";
        return char.ToUpperInvariant(word[0]) + word[1..].ToLowerInvariant();
    }

    public static string DefaultElaboration(GpoBaselineResult b) =>
        $"\"{b.Codename}\" is a single, coherent {(b.Strict ? "strict" : "balanced")} hardening baseline of " +
        $"{b.Rules.Count} Group Policy rules that reinforce one another as defense in depth. Strong " +
        "password and lockout policy raise the cost of credential attacks; the security options and " +
        "network rules (NTLMv2-only, SMB signing, no LM hash, WDigest off, SMBv1 removed) close the " +
        "common lateral-movement and credential-theft paths; the audit policy makes every relevant " +
        "event observable; the firewall enforces default-deny inbound; and Defender plus the admin " +
        "templates stop malware delivery and execution. No two rules conflict — each covers a distinct " +
        "layer, so together they form a complete, working baseline.";

    public static string ToMarkdown(GpoBaselineResult b, string elaboration)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# Cybersecurity Group Policy Baseline — {b.Codename}").AppendLine();
        sb.AppendLine($"_{(b.Strict ? "Strict" : "Balanced")} posture · {b.Rules.Count} rules that work together._").AppendLine();
        sb.AppendLine(elaboration.Trim()).AppendLine();
        sb.AppendLine($"## {b.Rules.Count} Group Policy rules").AppendLine();
        sb.AppendLine("| # | Category | Policy | Value | Why |");
        sb.AppendLine("|---|----------|--------|-------|-----|");
        for (int i = 0; i < b.Rules.Count; i++)
        {
            var r = b.Rules[i];
            sb.AppendLine($"| {i + 1} | {r.Category} | {r.Policy} | {r.Value} | {r.Why} |");
        }
        return sb.ToString();
    }
}
