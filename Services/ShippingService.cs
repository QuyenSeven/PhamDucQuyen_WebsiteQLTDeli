using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace QLyNhaHangTDeli.Services
{
    public static class ShippingService
    {
        // Tọa độ cố định của cửa hàng (Vinata 2B - 289 Khuất Duy Tiến)
        private const double StoreLat = 21.002795;
        private const double StoreLng = 105.799879;

        public static async Task<double> GetDistanceFromCoordinatesAsync(double customerLat, double customerLng)
        {
            using (var client = new HttpClient())
            {
                // OSRM API expects coordinates as lon,lat pairs separated by ;
                string url = $"http://router.project-osrm.org/route/v1/driving/{StoreLng},{StoreLat};{customerLng},{customerLat}?overview=false";

                var response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    dynamic result = JsonConvert.DeserializeObject(json);

                    // Lấy khoảng cách (meters) từ routes[0].distance
                    double distanceInMeters = result.routes[0].distance;
                    return distanceInMeters / 1000.0; // km
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new Exception($"OSRM API lỗi: {response.StatusCode} - {error}");
                }
            }
        }

        public static double CalculateShippingFee(double distance)
        {
            return distance * 5000; // 5.000đ mỗi km
        }
    }
}
