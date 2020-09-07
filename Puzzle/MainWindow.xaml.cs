using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Puzzle
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Image[,] Board; //mảng ánh xạ màn hình chơi
        private DispatcherTimer dispatcherTimer;
        private int TimeLeft;
        private int level = 3;
        int startX = 10, startY = 10; 
        List<Image> ListImages; //danh sách lưu các mảnh của game
        Display viewModel;   //Dùng cho việc binding các thành phần giao diện

        private int side = 420; //maxWidth, maxHeight        
        System.Windows.Threading.DispatcherTimer Timer = new System.Windows.Threading.DispatcherTimer();
        public MainWindow()
        {
            InitializeComponent();
            viewModel = (Display)DataContext;
            ListImages = new List<Image>();
            Level.SelectedItem = viewModel.Levels[0];
           
        }




        //hàm resize hình ảnh thành hình vuông 
        //Tham khảo tại nguồn : https://dlaa.me/blog/post/6129847 Author: David Anson
        private ImageSource CreateResizedImage(ImageSource source, int width, int height)
        {
            // Target Rect for the resize operation
            Rect rect = new Rect(0, 0, width, height);

            // Create a DrawingVisual/Context to render with
            DrawingVisual drawingVisual = new DrawingVisual();
            using (DrawingContext drawingContext = drawingVisual.RenderOpen())
            {
                drawingContext.DrawImage(source, rect);
            }

            // Use RenderTargetBitmap to resize the original image
            RenderTargetBitmap resizedImage = new RenderTargetBitmap(
                (int)rect.Width, (int)rect.Height,  // Resized dimensions
                96, 96,                             // Default DPI values
                PixelFormats.Default);              // Default pixel format
            resizedImage.Render(drawingVisual);

            // Return the resized image
            return resizedImage;
        }

        //Hàm mở Dialog chọn hình ảnh để chơi
        private void openImage()
        {

            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "Image files (*.png;*.jpeg;*jpg)|*.png;*.jpeg;*.jpg|All files (*.*)|*.*";
            dialog.Multiselect = false;//không cho phép chọn nhiều ảnh
            if (dialog.ShowDialog() == true)
            {
                //Xoá các ảnh của lượt chơi trước nếu có 
                int count = PuzzleBox.Children.Count - 1;
                while (PuzzleBox.Children.Count > 1)
                {
                    PuzzleBox.Children.RemoveAt(count);
                    count--;
                }

                var bitmapSource = new BitmapImage(new Uri(dialog.FileName));
                var bitmap = CreateResizedImage(bitmapSource, 320, 320);

                //Hiển thị hình ảnh gốc 
                viewModel.OriginImage = bitmap;

                bitmap = CreateResizedImage(bitmapSource, side, side);
                //Tạo ảnh chuẩn bị cho việc cắt
                viewModel.PuzzleImage = bitmap;
                viewModel.PathImage = dialog.FileName;                              
                CropImage(level, side / level);
                ShufflePicture(level);
                StartTimer(3, 15);//Thời gian chơi được tạo là 3 phút 15 giây -- chỉnh sửa thời gian chơi ở đây
            }

        }


        //Hàm cắt ảnh với n là số mảnh mỗi dòng và mỗi cột ; size là kích thước cạnh của 1 mảnh
        private void CropImage(int n, int size)
        {
            if (Board != null)
            {
                Array.Clear(Board, 0, Board.Length);

            }

            Board = new Image[n, n];
            ListImages.Clear();

            
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    var rect = new Int32Rect(j * size, i * size, size, size);

                    var cropBitmap = new CroppedBitmap(viewModel.PuzzleImage as BitmapSource, rect);
                    var cropImage = new Image();
                    cropImage.Stretch = Stretch.None;
                    cropImage.Width = size - 1;
                    cropImage.Height = size - 1;
                    cropImage.Source = cropBitmap;
                    ListImages.Add(cropImage);
                    cropImage.Tag = (i * n) + (j + 1);

                    if (!((i == n - 1) && (j == n - 1)))
                    {
                        
                        PuzzleBox.Children.Add(cropImage);
                        Canvas.SetLeft(cropImage, startX + j * size);
                        Canvas.SetTop(cropImage, startY + i * size);
                        Board[i, j] = cropImage;
                        cropImage.MouseLeftButtonUp += CropImage_MouseLeftButtonUp;


                    }

                }
            }
        }


        //Hàm xử lý khi người chơi nhấp chuột trái vào một ảnh 
        private void CropImage_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var position = e.GetPosition(this); 

            int i = ((int)position.Y - startY - 20) / (side / level);
            int j = ((int)position.X - startX - 20) / (side / level);
            if (CheckMove(i - 1, j, level))
            {
                MoveItem(i, j, i - 1, j);
                ShowWinMessage();

            }
            else if (CheckMove(i, j + 1, level))
            {
                MoveItem(i, j, i, j + 1);
                ShowWinMessage();


            }
            else if (CheckMove(i + 1, j, level))
            {
                MoveItem(i, j, i + 1, j);
                ShowWinMessage();

            }
            else if (CheckMove(i, j - 1, level))
            {
                MoveItem(i, j, i, j - 1);
                ShowWinMessage();
            }

        }


        //Hàm xử lý di chuyển một ảnh tới vị trí mới [i,j] là vị trí cũ [I,J] là vị trí mới
        private void MoveItem(int i, int j, int I, int J)
        {
            Storyboard sb = new Storyboard();
            DoubleAnimation da = new DoubleAnimation();

            if (i == I)
            {
                da.From = startX + j * (side / level);
                da.By = startX + J * (side / level) - da.From;

            }
            else
            {
                da.From = startX + i * (side / level);
                da.By = startX + I * (side / level) - da.From;

            }

            da.Duration = new Duration(TimeSpan.FromSeconds(.2));
            sb.Children.Add(da);


            object prop = I == i ? Canvas.LeftProperty : Canvas.TopProperty;

            Storyboard.SetTargetProperty(da, new PropertyPath(prop));

            //Tìm mảnh cần di chuyển 
            foreach (FrameworkElement element in PuzzleBox.Children)
            {
                if (element.Equals(Board[i, j]))
                    sb.Begin(element);
            }

            Board[I, J] = Board[i, j];
            Board[i, j] = null;



        }

        //Hàm kiểm tra vị trí chuẩn bị di chuyển có hợp lệ
        private bool CheckMove(int i, int j, int n)
        {
            if (i < 0 || j < 0 || i > n - 1 || j > n - 1) return false;
            return (Board[i, j] == null);
        }


        //Hàm tạo một game mới
        private void NewGame_Click(object sender, RoutedEventArgs e)
        {
            openImage();
           
        }

        //Hàm tạo TimeCountdown
        public void StartTimer(int minutes, int seconds)
        {

            TimeLeft = minutes * 60 + seconds;
            dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += _timer_Tick;
            dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
            dispatcherTimer.Start();

        }


        //Hàm xử lý mỗi giây trôi qua
        private void _timer_Tick(object sender, EventArgs e)
        {
            if (TimeLeft > 0)
            {
                TimeLeft--;
                var timeleft = TimeSpan.FromSeconds(TimeLeft);
                TimeCountdown.Content = timeleft.ToString(@"mm\:ss");

            }
            else
            {
                dispatcherTimer.Stop();
                MessageBox.Show("GAME OVER!", "Notify", MessageBoxButton.OK);
                Reset();

            }
        }


        //Hàm thoát game
        private void Quit_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("Do you want to exit this game?", "Exit Game", MessageBoxButton.YesNoCancel, MessageBoxImage.Question, MessageBoxResult.No);
            switch (result)
            {
                case MessageBoxResult.Yes:
                    this.Close();
                    break;
                case MessageBoxResult.No:
                    break;

            }
        }

        //Hàm Load game 
        private void LoadGame_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "My Puzzle files (*.mpzz)|*.mpzz|All files (*.*)|*.*";
            //mở file game đã lưu
            if (dialog.ShowDialog() == true)
            {
               Reset();
               using (StreamReader reader = new StreamReader(dialog.FileName))
                {
                    //đọc đương dẫn ảnh ở dòng đầu tiên
                    string path = reader.ReadLine(); 

                    //Kiểm tra ảnh còn tồn tại hay bị di chuyển hoặc bị xoá
                    if (File.Exists(path)) 
                    {
                        var bitmapSource = new BitmapImage(new Uri(path));
                        var bitmap = CreateResizedImage(bitmapSource, 320, 320);
                        //Hiển thị hình ảnh gốc 
                        viewModel.OriginImage = bitmap;

                        bitmap = CreateResizedImage(bitmapSource, side, side);
                        //Tạo ảnh chuẩn bị cho việc cắt
                        viewModel.PuzzleImage = bitmap;
                        viewModel.PathImage = path;
                        
                       

                        if (Int32.TryParse(reader.ReadLine(), out level)) //đọc level game 
                        {
                            CropImage(level, side / level); //cắt ảnh
                            int  i, j;
                            string line;
                            string[] token;
                            //bắt đầu đọc vị trí của các mảnh và sắp xếp nó vào vị trí đúng như đã lưu
                            while (!reader.EndOfStream)
                            {
                                line = reader.ReadLine();
                                token = line.Split(new char[] { ';' });

                              
                                if (token[0] != "0")
                                {
                                    for(int k=1;k<PuzzleBox.Children.Count;k++)
                                    {
                                        var item = PuzzleBox.Children[k] as Image;

                                        if (item.Tag != null)
                                        {
                                            string tag = item.Tag.ToString();
                                            if (tag == token[0])
                                            {
                                                Int32.TryParse(token[1], out i);
                                                Int32.TryParse(token[2], out j);
                                                Canvas.SetLeft(item, j * (side / level) + startX);
                                                Canvas.SetTop(item, i * (side / level) + startY);
                                                Board[i, j] = item;
                                            }


                                        }
                                    }
                                    
                                }
                                else
                                {
                                    //Nếu vị trí ô trống thì xoá ảnh tại vị trí đó trên Board
                                    Int32.TryParse(token[1], out i);
                                    Int32.TryParse(token[2], out j);
                                    Board[i, j] = null;
                                }
                               
                              
                              

                            }


                            MessageBox.Show("Successfully Loading", "Loading Message", MessageBoxButton.OK, MessageBoxImage.None);
                            StartTimer(3, 15);
                            
                        }
                        else
                        {
                            MessageBox.Show("Error in reading!", "Error Message", MessageBoxButton.OK, MessageBoxImage.Error);
                            
                        }



                    }
                    else {
                        MessageBox.Show("Image had been removed!", "Error Message",MessageBoxButton.OK,MessageBoxImage.Error);
                        
                    }

                }

            }
        }
           



        //Hàm xử lý Click vào button Shuffle
        private void Shuffle_Click(object sender, RoutedEventArgs e)
        {
            ShufflePicture(level);
        }



        //Hàm xáo trộn hình ảnh trong màn hình chơi
        private void ShufflePicture(int n)
        {
           
            Random rand = new Random();

            int i, j;
            int len;
            //nếu level thấp thì số lần trộn ít, càng tăng cao thì tăng số lần trộn
            if (n < 5)
                len = 100 * n;
            else if (n > 4 && n < 10)
                len = 500 * n;
            else
                len = 1000 * n;

            //di chuyển random
            for (int k = 0; k < len; k++)
            {
                 i = rand.Next(n);
                 j = rand.Next(n);

                
                if (CheckMove(i - 1, j, level)) //di chuyển qua lên trên
                {
                    MoveItem(i, j, i - 1, j);
                }
                else if (CheckMove(i, j + 1, level))  //di chuyển qua phải
                {
                    MoveItem(i, j, i, j + 1);
                }
                else if (CheckMove(i + 1, j, level)) //di chuyển xuống dưới
                {
                    MoveItem(i, j, i + 1, j);
                }
                else if (CheckMove(i, j - 1, level)) //di chuyển qua trái
                {
                    MoveItem(i, j, i, j - 1);
                }
                
            }
        }

        //Hàm kiểm tra thắng game 
        /*Giả thích cách kiểm tra: Mảng hai chiều Board[][] đại diện cho các mảnh trên màn hình chơi, 
        ListImage chứa các mảnh ghép của mảng hình ảnh ban đầu theo chiều dài danh sách ví dụ ListImage[0]= a[0,0],ListImage[1]=a[0,1]*/
        private bool CheckWin()
        {
            int k = 0;
            for(int i=0;i<level;i++)
            {
                for(int j=0;j<level;j++)
                {
                    if(!(i==level-1 && j== level-1))
                    {
                        if (!(Board[i, j] == ListImages[k++]))
                            return false;
                    }
                }
            }
            return true;
        }


       //Hàm xử lý thắng game
        private void ShowWinMessage()
        {
            if (CheckWin())
            {
                dispatcherTimer.Stop();

                //Hiện ra mảnh ghép cuối cùng trong 3s -- mảnh cuối cùng được lưu ở cuối danh sách ListImages (cái này làm cho vui ạ)
                var lastImg = new Image();
                lastImg = ListImages[ListImages.Count - 1];
                PuzzleBox.Children.Add(lastImg);
                int i = level - 1;
                Canvas.SetLeft(lastImg,i * (side / level) + startX);
                Canvas.SetTop(lastImg,i * (side / level) + startY);


                DoubleAnimation animation = new DoubleAnimation();
                Storyboard storyboard = new Storyboard();
                animation.From = 0;
                animation.To = 1;
                animation.Duration = new Duration(TimeSpan.FromSeconds(3));
                storyboard.Children.Add(animation);
                
                Storyboard.SetTargetProperty(animation, new PropertyPath(Canvas.OpacityProperty));
                storyboard.Completed += Storyboard_Completed; //Thông báo thắng game
                storyboard.Begin(lastImg);
                
               
               
            }
        }

        private void Storyboard_Completed(object sender, EventArgs e)
        {
            MessageBox.Show("Congratulation! You Win !!! \n Let's try playing high level :3", "Message",MessageBoxButton.OK);
            Reset();
        }

        //Hàm lưu game 
        private void SaveGame_Click(object sender, RoutedEventArgs e)
        {
            //Kiểm tra có game nào đang chơi và game chưa kết thúc -->lưu game 
            if (viewModel.OriginImage != null && TimeLeft>0)  
            {
                SaveFileDialog dialog = new SaveFileDialog();

                //lưu game trong tệp có đuôi .mpzz
                dialog.Filter = "My Puzzle files (*.mpzz)|*.mpzz|All files (*.*)|*.*"; 
                if (dialog.ShowDialog() == true)
                {
                    using (StreamWriter writer = new StreamWriter(dialog.FileName))
                    {
                        writer.WriteLine(viewModel.PathImage); //ghi đường dẫn hình ảnh
                        writer.WriteLine(level.ToString()); //ghi level của game
                       
                            for (int i = 0; i < level; i++)
                            {
                                for (int j = 0; j < level; j++)
                                {
                                if (!(Board[i, j] == null)) //kiểm tra có phải ô trống 
                                    writer.WriteLine($"{Board[i, j].Tag.ToString()};{i};{j}"); //ghi lại mảnh thứ mấy và vị trí của mảnh đó
                                else
                                    writer.WriteLine($"0;{i};{j}"); //ghi lại vị trí ô trống với mảnh = 0
                                }
                            }
                        
                    }
                    MessageBox.Show("Successfully saved", "Save Game");
                }
            }
            else
            {
                MessageBox.Show("Can't save game", "Save Game");
            }

        }

        //Hàm xử lý khi người chơi chọn level khác trong combobox
        private void Level_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            //Lấy nội dung người chơi đã chọn
            string temp = viewModel.SLevel.Name;
            //Tách chuỗi ví dụ : 3 x 3 tách lấy số 3
            string[] token = temp.Split(new char[] { ' ' });
            int _level = 3;

            //Chuyển kí tự sang chuỗi nếu xảy ra lỗi không thể chuyển được thì để nguyên level = 3
            if (Int32.TryParse(token[0], out _level))
            {
                if(_level!=level) //Kiểm tra người chơi có chọn level hiện tại lại không ví dụ đang ở level 5 x 5, chọn 5 x 5 thì không làm gì cả
                {
                    level = _level;
                    if (viewModel.OriginImage != null) //Kiểm tra đã có màn chơi chưa
                    {
                        //Xoá các ảnh của lượt chơi trước nếu có 
                        int count = PuzzleBox.Children.Count - 1;
                        while (PuzzleBox.Children.Count > 1)
                        {
                            PuzzleBox.Children.RemoveAt(count);
                            count--;
                        }

                        //Tạo màn chơi mới với level người chơi đã chọn
                        CropImage(level, side / level);
                        ShufflePicture(level);
                        StartTimer(3, 15);
                    }

                }
            }

        }

        //Hàm reset lại giao diện
        private void Reset()
        {
            //Xoá các ảnh của lượt chơi trước nếu có 
            int count = PuzzleBox.Children.Count - 1;
            while (PuzzleBox.Children.Count > 1)
            {
                PuzzleBox.Children.RemoveAt(count);
                count--;
            }

            //reset lại giao diện
            viewModel.OriginImage = null;
            viewModel.PuzzleImage = null;
            viewModel.PathImage = null;
        }
    }

       
}
