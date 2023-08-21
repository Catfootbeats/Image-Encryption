using System.Diagnostics;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Storage;
using ImageEncryption.Core;
using SkiaSharp;

namespace ImageEncryption
{
    public partial class MainPage : ContentPage
    {
        private FileResult original_file;
        private FileResult output_file;
        public bool isProcess;

        public MainPage()
        {
            InitializeComponent();
        }

        private async void CryptionBtnClicked(object sender, EventArgs e)
        {
            if (isProcess)
            {
                CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

                string text = "我知道你很急，但是你先别急。";
                ToastDuration duration = ToastDuration.Short;
                double fontSize = 14;
                var toast = Toast.Make(text, duration, fontSize);
                await toast.Show(cancellationTokenSource.Token);
                return;
            }
            if (seed.Text.Length == 0)
            {
                CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

                string text = "请输入种子";
                ToastDuration duration = ToastDuration.Short;
                double fontSize = 14;
                var toast = Toast.Make(text, duration, fontSize);
                await toast.Show(cancellationTokenSource.Token);
                return;
            }
            if (original_file == null)
            {
                CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

                string text = "加载图片失败";
                ToastDuration duration = ToastDuration.Short;
                double fontSize = 14;
                var toast = Toast.Make(text, duration, fontSize);
                await toast.Show(cancellationTokenSource.Token);
                return;
            }
            isProcess = true;
            cryption_btn.Text = "处理中...";
            await EncryptionProcess();
            cryption_btn.Text = "完成！";
            isProcess = false;
        }

        private async void Original_Input_Btn_Clicked(object sender, EventArgs e)
        {
            if (isProcess)
            {
                CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

                string text = "我知道你很急，但是你先别急。";
                ToastDuration duration = ToastDuration.Short;
                double fontSize = 14;
                var toast = Toast.Make(text, duration, fontSize);
                await toast.Show(cancellationTokenSource.Token);
                return;
            }
            FileResult file;
            string action = await DisplayActionSheet("选择图片", "取消", null, "从相册选择", "拍摄图片");
            if (!string.IsNullOrEmpty(action))
            {
                file = await PickImgAsync(action);
                if (file != null)
                {
                    Debug.WriteLine(file.FullPath);
                    original_file = file;
                    Original_Input_Btn.Source = file.FullPath;
                    Encrypt_Input_Btn.Source = "";
                    cryption_btn.Text = "开始";
                }
            }
        }

        private async void Encrypt_Save_Btn_Clicked(object sender, EventArgs e)
        {
            if (output_file == null)
                return;
            if (isProcess)
            {
                CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

                string text = "我知道你很急，但是你先别急。";
                ToastDuration duration = ToastDuration.Short;
                double fontSize = 14;
                var toast = Toast.Make(text, duration, fontSize);
                await toast.Show(cancellationTokenSource.Token);
                return;
            }
            CancellationToken cancellationToken = new CancellationToken();
            using var stream = File.OpenRead(output_file.FullPath);
            var fileSaverResult = await FileSaver.Default.SaveAsync(output_file.FileName, stream, cancellationToken);
            if (fileSaverResult.IsSuccessful)
            {
                await Toast.Make($"保存成功: {fileSaverResult.FilePath}").Show(cancellationToken);
            }
            else
            {
                await Toast.Make($"保存失败: {fileSaverResult.Exception.Message}").Show(cancellationToken);
            }
        }

        /// <summary>
        /// 线程方法
        /// </summary>
        private async Task EncryptionProcess()
        {
            SeedRandom random = new(int.Parse(seed.Text));
            FileResult result = await Task.Run(
                async () => { 
                    return await
                        Encrypt(
                            original_file, 
                            random.NextDouble(3.57, 4), 
                            random.NextDouble(0.001, 0.999)
                    );}
            );
            Encrypt_Input_Btn.Source = result.FullPath;
            output_file = result;
        }

        private async Task<FileResult> PickImgAsync(string action)
        {
            FileResult file = null;
            if (action == "从相册选择")
            {
                file = await MediaPicker.Default.PickPhotoAsync();
                Debug.WriteLine(file.FullPath);
            }
            if(action == "拍摄图片")
            {
                if (MediaPicker.Default.IsCaptureSupported)
                {
                    FileResult photo = await MediaPicker.Default.CapturePhotoAsync();

                    if (photo != null)
                    {
                        // save the file into local storage
                        string localFilePath = Path.Combine(FileSystem.CacheDirectory, photo.FileName);

                        using Stream sourceStream = await photo.OpenReadAsync();
                        using FileStream localFileStream = File.OpenWrite(localFilePath);

                        await sourceStream.CopyToAsync(localFileStream);
                        file = photo;
                    }
                }
                else
                {
                    await DisplayAlert("警告", "无法使用摄像头", "确认");
                }
            };
            return file;
        }

        private double logistic(double u, double x, int n)
        {
            for (int i = 0; i < n; i++)
            {
                x = u * x * (1 - x);
            }
            return x;
        }

        /// <summary>
        /// 基于Logistic模型的混沌加解密
        /// </summary>
        /// <param name="src">要处理的图像数据</param>
        /// <param name="u">应属于[3.57,4]</param>
        /// <param name="x0">应属于(0,1)</param>
        /// <returns></returns>
        public async Task<FileResult> Encrypt(FileResult src, double u, double x0)
        {
            FileResult result;
            SKBitmap bitmap;
            using (Stream stream = await src.OpenReadAsync())
            {

                bitmap = SKBitmap.Decode(stream);
            }
            SKBitmap dest = new(bitmap.Width, bitmap.Height);
            int temp = Convert.ToInt32(3141 * u * u * u);
            double x = logistic(u, x0, temp);
            int key = Convert.ToInt32(Math.Floor((Math.Sqrt(x) * temp)) % 256);
            SKColor srcColor;
            for (int i = 0; i < bitmap.Width; i++)
            {
                if (i % 15 != 0)
                {
                    for (int j = 0; j < bitmap.Height; j++)
                    {
                        if (j % 15 != 0)
                        {
                            srcColor = bitmap.GetPixel(i, j);
                            x = logistic(u, x, Convert.ToInt32(u * u));
                            key = Convert.ToInt32(Math.Floor((Math.Sqrt(x) * temp)) % 256);
                            int r = key ^ srcColor.Red;//红色组件值
                            x = logistic(u, x, Convert.ToInt32(Math.Sqrt(u)));
                            key = Convert.ToInt32(Math.Floor(Math.Sqrt(x) * temp)) % 256;
                            int g = key ^ srcColor.Green;//绿色组件值
                            x = logistic(u, x, Convert.ToInt32(u));
                            key = Convert.ToInt32(Math.Floor(Math.Sqrt(x) * temp)) % 256;
                            int b = key ^ srcColor.Blue;//蓝色组件值

                            bitmap.SetPixel(i, j, new SKColor((byte)r, (byte)g, (byte)b));
                            //基于指定的（红色、绿色和蓝色）创建 Color 结构
                        }
                    }
                }
                else
                {
                    //cryption_btn.Text = $"{i / bitmap.Width}%";
                    //Debug.WriteLine((double)i / (double)bitmap.Width + "%");
                }
            }
            dest = bitmap;
            using (MemoryStream memStream = new MemoryStream())
            using (SKManagedWStream wstream = new SKManagedWStream(memStream))
            {
                dest.Encode(wstream, SKEncodedImageFormat.Png, 50);
                string localFilePath = Path.Combine(FileSystem.CacheDirectory, src.FileName);

                using FileStream localFileStream = File.OpenWrite(localFilePath);

                memStream.WriteTo(localFileStream);
                result = new FileResult(localFilePath);
            }
            return result;
        }

    }
}