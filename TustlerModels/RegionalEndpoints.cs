using System.Collections.ObjectModel;

namespace TustlerModels
{
    public class RegionalEndpointsViewModel
    {
        public ObservableCollection<Endpoint> Endpoints
        {
            get;
            private set;
        }

        public RegionalEndpointsViewModel()
        {
            var codes = new Endpoint[] {
                new Endpoint { Name = "US East (Ohio)", Code = "us-east-2" },

                new Endpoint { Name = "US East (N. Virginia)", Code = "us-east-1" },

                new Endpoint { Name = "US West (N. California)", Code= "us-west-1" },

                new Endpoint { Name = "US West (Oregon)", Code= "us-west-2" },

                new Endpoint { Name = "Asia Pacific (Hong Kong)", Code= "ap-east-1" },

                new Endpoint { Name = "Asia Pacific (Mumbai)", Code= "ap-south-1" },

                new Endpoint { Name = "Asia Pacific (Osaka-Local)", Code= "ap-northeast-3" },

                new Endpoint { Name = "Asia Pacific (Seoul)", Code= "ap-northeast-2" },

                new Endpoint { Name = "Asia Pacific (Singapore)", Code= "ap-southeast-1" },

                new Endpoint { Name = "Asia Pacific (Sydney)", Code= "ap-southeast-2" },

                new Endpoint { Name = "Asia Pacific (Tokyo)", Code= "ap-northeast-1" },

                new Endpoint { Name = "Canada (Central)", Code= "ca-central-1" },

                new Endpoint { Name = "Europe (Frankfurt)", Code= "eu-central-1" },

                new Endpoint { Name = "Europe (Ireland)", Code= "eu-west-1" },

                new Endpoint { Name = "Europe (London)", Code= "eu-west-2" },

                new Endpoint { Name = "Europe (Paris)", Code= "eu-west-3" },

                new Endpoint { Name = "Europe (Stockholm)", Code= "eu-north-1" },

                new Endpoint { Name = "Middle East (Bahrain)", Code= "me-south-1" },

                new Endpoint { Name = "South America (São Paulo)", Code= "sa-east-1" },
            };

            this.Endpoints = new ObservableCollection<Endpoint>(codes);
        }
    }

    public class Endpoint
    {
        public string Name { get; internal set; }
        public string Code { get; internal set; }
    }
}
