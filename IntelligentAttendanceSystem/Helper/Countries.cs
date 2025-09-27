using Microsoft.AspNetCore.Mvc.Rendering;

namespace IntelligentAttendanceSystem.Helper
{
    public static class Countries
    {
        public static readonly Dictionary<string, string> CountryList = new Dictionary<string, string>
    {
        {"PK", "Pakistan"},
        {"US", "United States"},
        {"CA", "Canada"},
        {"GB", "United Kingdom"},
        {"AU", "Australia"},
        {"DE", "Germany"},
        {"FR", "France"},
        {"IT", "Italy"},
        {"ES", "Spain"},
        {"JP", "Japan"},
        {"CN", "China"},
        {"IN", "India"},
        {"BR", "Brazil"},
        {"MX", "Mexico"},
        {"RU", "Russia"},
        {"KR", "South Korea"},
        {"SG", "Singapore"},
        {"MY", "Malaysia"},
        {"ID", "Indonesia"},
        {"TH", "Thailand"},
        {"VN", "Vietnam"},
        {"PH", "Philippines"},
        {"SA", "Saudi Arabia"},
        {"AE", "United Arab Emirates"},
        {"ZA", "South Africa"},
        {"NG", "Nigeria"},
        {"EG", "Egypt"},
        {"TR", "Turkey"},
        {"NL", "Netherlands"},
        {"SE", "Sweden"},
        {"NO", "Norway"},
        {"DK", "Denmark"},
        {"FI", "Finland"},
        {"BE", "Belgium"},
        {"CH", "Switzerland"},
        {"AT", "Austria"},
        {"PL", "Poland"},
        {"CZ", "Czech Republic"},
        {"GR", "Greece"},
        {"PT", "Portugal"},
        {"IE", "Ireland"},
        {"IL", "Israel"},
        // Add more countries as needed
    };

        public static List<SelectListItem> GetCountrySelectList()
        {
            return CountryList
                .Select(c => new SelectListItem { Value = c.Key, Text = c.Value })
                .OrderBy(c => c.Text)
                .ToList();
        }

        public static string GetCountryName(string countryCode)
        {
            return CountryList.ContainsKey(countryCode) ? CountryList[countryCode] : countryCode;
        }
    }
}
