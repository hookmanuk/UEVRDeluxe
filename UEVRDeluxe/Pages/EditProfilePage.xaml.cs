#region Usings
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UEVRDeluxe.Code;
using UEVRDeluxe.ViewModels;
using Windows.Storage.Pickers;
#endregion

namespace UEVRDeluxe.Pages;

public sealed partial class EditProfilePage : Page {
	EditProfilePageVM VM = new();

	public EditProfilePage() {
		this.InitializeComponent();
		this.Loaded += EditProfilePage_Loaded;
	}

	protected override void OnNavigatedTo(NavigationEventArgs e) {
		base.OnNavigatedTo(e);

		VM.GameInstallation = e.Parameter as GameInstallation;
	}

	void EditProfilePage_Loaded(object sender, RoutedEventArgs e) {
		VM.LocalProfile = LocalProfile.FromUnrealVRProfile(VM.GameInstallation.EXEName, true);

		SetRadioButtonValue(spRenderingMethod, VM.LocalProfile.Config.Global["VR_RenderingMethod"]);
		SetRadioButtonValue(spSyncedSequentialMethod, VM.LocalProfile.Config.Global["VR_SyncedSequentialMethod"]);
		cbEnableDepth.IsChecked = bool.Parse(VM.LocalProfile.Config.Global["VR_EnableDepth"] ?? "false");
		slResolutionScale.Value = Math.Round(double.Parse(VM.LocalProfile.Config.Global["OpenXR_ResolutionScale"] ?? "1.0", CultureInfo.InvariantCulture) * 100);
	}

	async void Save_Click(object sender, RoutedEventArgs e) {
		try {
			VM.IsLoading = true;

			await SaveAsync();
			VM.IsLoading = false;

			Frame.GoBack();
		} catch (Exception ex) {
			VM.IsLoading = false;

			await new ContentDialog {
				Title = "Save error", Content = ex.Message, CloseButtonText = "OK", XamlRoot = this.XamlRoot
			}.ShowAsync();
		}
	}

	async Task SaveAsync() {
		VM.LocalProfile.Config.Global["VR_RenderingMethod"] = GetRadioButtonValue(spRenderingMethod);
		VM.LocalProfile.Config.Global["VR_SyncedSequentialMethod"] = GetRadioButtonValue(spSyncedSequentialMethod);
		VM.LocalProfile.Config.Global["VR_EnableDepth"] = cbEnableDepth.IsChecked.ToString();
		VM.LocalProfile.Config.Global["OpenXR_ResolutionScale"] = Math.Round(slResolutionScale.Value / 100, 2).ToString(CultureInfo.InvariantCulture);

		await VM.LocalProfile.SaveAsync();
	}

	void SetRadioButtonValue(StackPanel parent, string tag) =>
		((RadioButton)parent.Children.First(c => (c as RadioButton)?.Tag?.ToString() == tag)).IsChecked = true;

	string GetRadioButtonValue(StackPanel parent) =>
		((RadioButton)parent.Children.First(c => (c as RadioButton)?.IsChecked ?? false)).Tag as string;

	async void Publish_Click(object sender, RoutedEventArgs e) {
		try {
			VM.IsLoading = true;

			byte[] profileZip = await VM.LocalProfile.PrepareForSubmitAsync(VM.GameInstallation);

			VM.IsLoading = false;

			var picker = new FileSavePicker();
			picker.FileTypeChoices.Add("ZIP File", [".zip"]);
			picker.DefaultFileExtension = ".zip";
			picker.SuggestedFileName = VM.GameInstallation.EXEName + ".zip";

			var hWnd = XamlRoot.ContentIslandEnvironment.AppWindowId;
			WinRT.Interop.InitializeWithWindow.Initialize(picker, (int)hWnd.Value);

			var saveFile = await picker.PickSaveFileAsync();
			if (saveFile != null) {
				using (var stream = await saveFile.OpenStreamForWriteAsync()) {
					await stream.WriteAsync(profileZip, 0, profileZip.Length);
				}
			}

		} catch (Exception ex) {
			VM.IsLoading = false;

			await new ContentDialog {
				Title = "Publish error", Content = ex.Message, CloseButtonText = "OK", XamlRoot = this.XamlRoot
			}.ShowAsync();
		}
	}

	void Back_Click(object sender, RoutedEventArgs e) => Frame.GoBack();
}
