namespace HttpMetrics;

public static class RouteMatcher
{
    public static Route? TryMatch(IEnumerable<Route> routes, Uri uri)
    {
        string[] segments = uri.AbsolutePath.Trim('/').Split('/');

        foreach (var route in routes)
        {
            var routeSegements = route.Template.Trim('/').Split('/');
            if (routeSegements.Length != segments.Length)
            {
                continue;
            }

            bool isMatch = true;
            for (int i = 0; i < routeSegements.Length; i++)
            {
                var routeSegement = routeSegements[i];

                if (routeSegement.StartsWith('{') && routeSegement.EndsWith('}'))
                {
                    continue;
                }

                if (!string.Equals(segments[i], routeSegement, StringComparison.OrdinalIgnoreCase))
                {
                    isMatch = false;
                    continue;
                }
            }

            if (isMatch)
            {
                // Console.WriteLine(route.Name);
                return route;
            }
        }

        return null;
    }
}
