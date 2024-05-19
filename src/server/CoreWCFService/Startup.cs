using CoreWCF;
using CoreWCF.Channels;
using CoreWCF.Configuration;
using CoreWCF.Description;
using CoreWCFService.onvif;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace CoreWCFService
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddServiceModelServices();
#if(!NoWsdl)
            services.AddServiceModelMetadata();
            services.AddSingleton<IServiceBehavior, UseRequestHeadersForMetadataAddressBehavior>();
#endif
            services.AddHttpContextAccessor();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app)
        {
            app.UseServiceModel(serviceBuilder =>
            {
                serviceBuilder.AddService<OnvifDeviceMgmtImpl>();
#if (NoHttps)
                serviceBuilder.AddServiceEndpoint<OnvifDeviceMgmtImpl, Device>(new WSHttpBinding(), "/Service.svc");
#else
                serviceBuilder.AddServiceEndpoint<OnvifDeviceMgmtImpl, Device>(new WSHttpBinding(SecurityMode.Transport), "/Service.svc");
#endif
#if (!NoWsdl)
                var serviceMetadataBehavior = app.ApplicationServices.GetRequiredService<ServiceMetadataBehavior>();
#if (NoHttps)
                serviceMetadataBehavior.HttpGetEnabled = true;
#else
                serviceMetadataBehavior.HttpsGetEnabled = true;
#endif
#endif
            });
        }
    }
}
