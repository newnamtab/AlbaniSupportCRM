namespace API.Settings
{
    public static class IOptionsRegistration
    {
        public static T RegisterOptions<T>(IServiceCollection services, IConfiguration configuration) where T : class
        {
            var sectionName = typeof(T).Name;
            var section = configuration.GetSection(sectionName);
            var options = section.Get<T>();
            services.Configure<T>(section);
            return options;
        }
    }
}
