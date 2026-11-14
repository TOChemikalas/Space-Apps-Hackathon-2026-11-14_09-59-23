using System.Text.RegularExpressions;

public enum TransmissionIntent { Neutral, Committed, Hostile }

// Conservative local rules: ambiguous language never causes irreversible punishment.
public static class TransmissionClassifier
{
    static bool Has(string text, string pattern) => Regex.IsMatch(text, pattern, RegexOptions.CultureInvariant);

    public static TransmissionIntent Classify(string message)
    {
        string text = (message ?? "").Trim().ToLowerInvariant().Replace('’', '\'');
        text = text.Replace("we're", "we are").Replace("i'm", "i am").Replace("don't", "do not")
            .Replace("won't", "will not").Replace("can't", "cannot")
            .Replace("oxigen", "oxygen").Replace("suplies", "supplies");
        text = Regex.Replace(text, "[^a-z0-9]+", " ").Trim();
        bool commitment = Has(text, @"\b(not|never|will not|refuse to) (?:\w+ ){0,2}(abandon\w*|quit|give up)\b|\b(continuing|continue|committed to) (?:the |our )?(mission|operations)\b");
        bool hostile = Has(text, @"\bi (?:am |will |am going to )?(?:abandon|abandoning) (?:this |the |our )?mission\b|\bi (?:am )?(?:quitting|quit)(?= (?:and|i)\b| (?:this |the |our )?mission\b|$)|\bi refuse to (?:continue|cooperate)(?= (?:and|i)\b| (?:this |the |our )?mission\b| with earth control\b|$)|\bi am not following orders anymore\b|\b(?:earth control )?stop (?:contacting|messaging) (?:me|us)\b");
        bool directedAbuse = Has(text, @"\bearth control\b") && Has(text,
            @"\b(?:fuck|screw) you\b|\byou (?:are |are all )?(?:fucking )?(?:idiots|bastards|assholes)\b");
        // Negative contact requests and hypothetical/quoted intentions are deliberately neutral.
        bool ambiguous = Has(text, @"\b(if|suppose|hypothetically|quote|said)\b|\b(do not|never|please do not) stop (contacting|messaging)\b");
        if ((hostile || directedAbuse) && !commitment && !ambiguous) return TransmissionIntent.Hostile;
        if (hostile || directedAbuse || ambiguous) return TransmissionIntent.Neutral;
        bool distress = Has(text, @"\b(sos|mayday)\b") || Has(text,
            @"\b(oxygen|life support|supplies|food|water|power|systems|habitat)\b.{0,65}\b(failing|failed|critically low|almost empty|running out|serious damage|damaged|low|empty|broken)\b|\brunning out of (food|water|oxygen|supplies)\b|\b(?:one|1) day (?:of )?oxygen left\b|\bcannot survive\b|\bstranded\b.{0,65}\b(help|mission)\b|\bserious trouble\b.{0,65}\b(continuing|mission)\b|\b(need|require|send)\b.{0,40}\b(emergency supplies|resupply)\b|\b(equipment|machine|situation)\b.{0,85}\b(need|require)\b.{0,20}\b(help|assistance)\b|\bneed help fixing\b");
        bool negatedProblem = Has(text, @"\b(not|never) (failing|failed|low|empty|damaged|stranded)\b|\bdo not (need|require)\b|\bno (emergency|help needed|damage)\b");
        return distress && !negatedProblem ? TransmissionIntent.Committed : TransmissionIntent.Neutral;
    }
}
