namespace MomOi.API.Models
{
    public enum Gender
    {
        Male,
        Female
    }

    public enum BleedingStatus
    {
        None,
        Light,
        Medium,
        Heavy
    }

    public enum MealType
    {
        Breakfast,
        Lunch,
        Dinner,
        Snack
    }

    public enum AllergySeverity
    {
        Mild,
        Moderate,
        Severe
    }

    public enum StressLevel
    {
        Low,
        Moderate,
        High
    }

    public enum AdherenceStatus
    {
        Taken,
        Skipped,
        Reminded
    }

    public enum SenderType
    {
        User,
        Bot,
        Expert
    }

    public enum Difficulty
    {
        Easy,
        Medium,
        Hard
    }

    public enum UrgencyLevel
    {
        Low,
        Medium,
        High,
        Critical
    }

    public enum DietPlanSource
    {
        Manual,
        AiGenerated,
        NutritionApi
    }

    public enum MaternalLifestyleProfile
    {
        Exhausted, // Kiệt sức
        Sedentary, // Ít vận động
        Balanced,  // Cân bằng
        Active,    // Năng động
        Unknown    // Chưa phân loại
    }
}
