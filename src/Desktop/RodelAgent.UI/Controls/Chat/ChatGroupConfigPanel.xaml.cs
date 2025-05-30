// Copyright (c) Richasy. All rights reserved.

using Richasy.WinUIKernel.Share.Toolkits;
using RodelAgent.Statics;
using RodelAgent.UI.Toolkits;
using RodelAgent.UI.ViewModels.Core;
using RodelAgent.UI.ViewModels.Items;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;

namespace RodelAgent.UI.Controls.Chat;

public sealed partial class ChatGroupConfigPanel : ChatGroupConfigPanelBase
{
    private bool _avatarChanged;
    private bool _isEmojiAvatar;
    private EmojiItem? _selectedEmoji;

    public ChatGroupConfigPanel() => InitializeComponent();

    /// <summary>
    /// 保存头像到本地.
    /// </summary>
    /// <returns><see cref="Task"/>.</returns>
    public async Task SaveAvatarAsync()
    {
        if (Cropper.Visibility == Visibility.Collapsed || !_avatarChanged || _isEmojiAvatar)
        {
            return;
        }

        var avatarPath = AppToolkit.GetPresetAvatarPath(ViewModel.Group!.Id);
        if (!File.Exists(avatarPath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(avatarPath)!);
            await File.Create(avatarPath).DisposeAsync();
        }

        var file = await StorageFile.GetFileFromPathAsync(avatarPath);
        using var stream = await file.OpenAsync(FileAccessMode.ReadWrite);
        await Cropper.SaveAsync(stream, CommunityToolkit.WinUI.Controls.BitmapFileFormat.Png, true);
    }

    /// <summary>
    /// 验证是否有效.
    /// </summary>
    /// <returns>是否有效.</returns>
    public bool IsValid()
        => !string.IsNullOrEmpty(ViewModel.Name) && ViewModel.SelectedAgents.Count >= 2;

    /// <inheritdoc/>
    protected override async void OnControlLoaded()
    {
        ViewModel.InjectFunc(GetEmoji);
        if (!string.IsNullOrEmpty(ViewModel.Group!.Emoji))
        {
            _isEmojiAvatar = true;
            var emoji = EmojiStatics.GetEmojis().Find(x => x.Unicode == ViewModel.Group.Emoji);
            ShowEmoji(emoji!);
            return;
        }

        var avatarPath = AppToolkit.GetPresetAvatarPath(ViewModel.Group!.Id);
        if (File.Exists(avatarPath))
        {
            var file = await StorageFile.GetFileFromPathAsync(avatarPath);
            await InitializeCropperAsync(file);
        }
    }

    protected override void OnControlUnloaded()
        => AgentRepeater.ItemsSource = null;

    private string GetEmoji()
        => _selectedEmoji?.Unicode ?? string.Empty;

    private async void OnImageAreaTapped(object sender, TappedRoutedEventArgs e)
        => await PickImageFileAsync();

    private async void OnImageAreaDrop(object sender, DragEventArgs e)
    {
        if (e.DataView.Contains(StandardDataFormats.StorageItems))
        {
            var items = await e.DataView.GetStorageItemsAsync();
            if (items.Count > 0 && items[0] is StorageFile file)
            {
                if (file.ContentType.Contains("image", StringComparison.OrdinalIgnoreCase))
                {
                    _avatarChanged = true;
                    await InitializeCropperAsync(file);
                }
            }
        }
    }

    private async void OnReplaceImageButtonClick(object sender, RoutedEventArgs e)
        => await PickImageFileAsync();

    private void OnImageAreaDragOver(object sender, DragEventArgs e)
    {
        e.DragUIOverride.Caption = ResourceToolkit.GetLocalizedString(Models.Constants.StringNames.AvatarDropTip);
        e.DragUIOverride.IsCaptionVisible = true;
        e.AcceptedOperation = DataPackageOperation.Move;
    }

    private async Task PickImageFileAsync()
    {
        var image = await this.Get<IFileToolkit>().PickFileAsync(".png,.jpg,.bmp", this.Get<AppViewModel>().ActivatedWindow);
        if (image is null)
        {
            return;
        }

        _avatarChanged = true;
        await InitializeCropperAsync(image);
    }

    private async Task InitializeCropperAsync(StorageFile imageFile)
    {
        _isEmojiAvatar = false;
        _selectedEmoji = null;
        EmojiContainer.Visibility = Visibility.Collapsed;
        Cropper.Visibility = Visibility.Visible;
        ImagePlaceholderContainer.Visibility = Visibility.Collapsed;
        ReplaceImageButton.Visibility = Visibility.Visible;
        await Cropper.LoadImageFromFile(imageFile);
    }

    private void ShowEmoji(EmojiItem emoji)
    {
        _isEmojiAvatar = true;
        _selectedEmoji = emoji;
        EmojiContainer.Visibility = Visibility.Visible;
        EmojiBlock.Text = emoji.ToEmoji();
        Cropper.Visibility = Visibility.Collapsed;
        ReplaceImageButton.Visibility = Visibility.Visible;
        ImagePlaceholderContainer.Visibility = Visibility.Collapsed;
    }

    private void OnEmojiClick(object sender, Statics.EmojiItem e)
    {
        EmojiFlyout.Hide();
        ShowEmoji(e);
    }

    private void OnEmojiAvatarButtonClick(object sender, RoutedEventArgs e)
        => FlyoutBase.ShowAttachedFlyout(sender as FrameworkElement);

    private void OnBackupAgentClick(object sender, object e)
    {
        var data = (sender as ChatAgentItemControl)?.ViewModel;
        ViewModel.AddAgentCommand.Execute(data);
    }

    private void OnAddAgentButtonClick(object sender, RoutedEventArgs e)
        => FlyoutBase.ShowAttachedFlyout(sender as FrameworkElement);

    private void OnRemoveAgentItemClick(object sender, RoutedEventArgs e)
    {
        var data = (sender as MenuFlyoutItem)?.Tag as ChatAgentItemViewModel;
        ViewModel.RemoveAgentCommand.Execute(data);
    }
}

public abstract class ChatGroupConfigPanelBase : LayoutUserControlBase<ChatGroupConfigViewModel>
{
    protected ChatGroupConfigPanelBase() => ViewModel = this.Get<ChatGroupConfigViewModel>();
}