using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using MomOi.API.Models.Health;

namespace MomOi.API.DTOs.Mom
{
    /// <summary>
    /// Partial update for the health profile. System.Text.Json only calls a setter when the
    /// property is present in the body, so each setter records it: a field that is omitted keeps
    /// its stored value, while a field sent as null clears it.
    /// </summary>
    public class UpdateMomHealthProfileDto
    {
        private readonly HashSet<string> _provided = new();

        public bool Has(string propertyName) => _provided.Contains(propertyName);

        private JourneyStage? _stage;
        public JourneyStage? Stage { get => _stage; set { _stage = value; _provided.Add(nameof(Stage)); } }

        private int? _pregnancyWeek;
        public int? PregnancyWeek { get => _pregnancyWeek; set { _pregnancyWeek = value; _provided.Add(nameof(PregnancyWeek)); } }

        private float? _bmi;
        public float? Bmi { get => _bmi; set { _bmi = value; _provided.Add(nameof(Bmi)); } }

        private float? _height;
        [Range(50, 250, ErrorMessage = "Chiều cao phải trong khoảng 50-250 cm.")]
        public float? Height { get => _height; set { _height = value; _provided.Add(nameof(Height)); } }

        private float? _weight;
        [Range(20, 300, ErrorMessage = "Cân nặng phải trong khoảng 20-300 kg.")]
        public float? Weight { get => _weight; set { _weight = value; _provided.Add(nameof(Weight)); } }

        private float? _bloodSugarLevel;
        public float? BloodSugarLevel { get => _bloodSugarLevel; set { _bloodSugarLevel = value; _provided.Add(nameof(BloodSugarLevel)); } }

        private bool? _hasGestDiabetes;
        public bool? HasGestDiabetes { get => _hasGestDiabetes; set { _hasGestDiabetes = value; _provided.Add(nameof(HasGestDiabetes)); } }

        private string[]? _medicalConditions;
        public string[]? MedicalConditions { get => _medicalConditions; set { _medicalConditions = value; _provided.Add(nameof(MedicalConditions)); } }

        private int? _avgCycleLength;
        public int? AvgCycleLength { get => _avgCycleLength; set { _avgCycleLength = value; _provided.Add(nameof(AvgCycleLength)); } }

        private DateTime? _lastPeriodDate;
        public DateTime? LastPeriodDate { get => _lastPeriodDate; set { _lastPeriodDate = value; _provided.Add(nameof(LastPeriodDate)); } }

        private DateTime? _deliveryDate;
        public DateTime? DeliveryDate { get => _deliveryDate; set { _deliveryDate = value; _provided.Add(nameof(DeliveryDate)); } }

        private bool? _isBreastfeeding;
        public bool? IsBreastfeeding { get => _isBreastfeeding; set { _isBreastfeeding = value; _provided.Add(nameof(IsBreastfeeding)); } }
    }
}
