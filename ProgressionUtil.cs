using System.Threading.Tasks;
using PlayFab;

public class ProgressionUtil
{
	public static async Task WaitForMothershipSessionToken()
	{
		while (!MothershipClientContext.IsClientLoggedIn())
		{
			await Task.Delay(1000);
		}
	}

	public static async Task WaitForPlayFabSessionTicket()
	{
		while (!PlayFabClientAPI.IsClientLoggedIn())
		{
			await Task.Delay(1000);
		}
	}
}
