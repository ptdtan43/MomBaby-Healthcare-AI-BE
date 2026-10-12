using System;
using System.Linq;

namespace MomOi.API.Models.Health
{
    /// <summary>
    /// Represents an Edinburgh Postnatal Depression Scale (EPDS) screening assessment.
    /// </summary>
    public class EpdsAssessment : BaseEntity
    {
        /// <summary>
        /// Foreign key reference to MomHealthProfile.
        /// </summary>
        public int ProfileId { get; set; }

        /// <summary>
        /// Navigation property to MomHealthProfile.
        /// </summary>
        public MomHealthProfile Profile { get; set; } = null!;

        /// <summary>
        /// Collection of 10 answer values (0-3 score each) for the EPDS questionnaire.
        /// </summary>
        public int[] Answers { get; set; } = new int[10];

        /// <summary>
        /// Total score of all answered questions.
        /// </summary>
        public int TotalScore => Answers.Sum();

        public bool IsUrgent => IndicatesUrgency(Answers);

        /// <summary>
        /// Urgent when total >= 13, or when question 10 (self-harm thoughts) is anything
        /// other than "Never" (score 0) — the EPDS guideline requires immediate follow-up
        /// for item 10 regardless of the total.
        /// </summary>
        public static bool IndicatesUrgency(int[] answers) =>
            answers.Sum() >= 13 || (answers.Length >= 10 && answers[9] > 0);

        public static bool HasSelfHarmThoughts(int[] answers) =>
            answers.Length >= 10 && answers[9] > 0;

        /// <summary>
        /// Timestamp when the test was taken.
        /// </summary>
        public DateTime TakenAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gemini-generated insight based on the scores.
        /// </summary>
        public string? AiAnalysis { get; set; }
    }
}
