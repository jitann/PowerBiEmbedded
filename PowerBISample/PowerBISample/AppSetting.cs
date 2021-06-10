using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PowerBISample
{
	public class AppSettings
	{
		public PowerBISetting PowerBISetting { get; set; }
	}

	public class PowerBISetting
	{
		public string ClientID { get; set; }

		public string ClientSecret { get; set; }

		public string PowerBIApi { get; set; }

		public string PowerBITokenApi { get; set; }

		public string Tenant { get; set; }
		public string WorkspaceId { get; set; }
		public string ReportId { get; set; }
	}
	
}
