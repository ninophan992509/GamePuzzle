using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;

namespace Puzzle
{
    class Display:INotifyPropertyChanged
    {
        private ImageSource originImage;
        private ImageSource puzzleImage;
        private String pathImage;
        public ImageSource OriginImage
        {
            get { return originImage; }
            set { originImage = value; OnPropertyChanged("OriginImage"); }
        }

        public ImageSource PuzzleImage
        {
            get { return puzzleImage; }
            set { puzzleImage = value; OnPropertyChanged("PuzzleImage"); }
        }

        public String PathImage
        {
            get { return pathImage; }
            set { pathImage = value; OnPropertyChanged("PathImage"); }
        }

        public Display()
        {
            Levels = new ObservableCollection<Level>()
            {
                 new Level(){Name="3 x 3"}
                ,new Level(){Name="4 x 4"}
                ,new Level(){Name="5 x 5"}
                ,new Level(){Name="6 x 6"}
                ,new Level(){Name="7 x 7"}
                ,new Level(){Name="8 x 8"}
                ,new Level(){Name="9 x 9"}
                ,new Level(){Name="10 x 10"}
                ,new Level(){Name="15 x 15"}
                ,new Level(){Name="20 x 20"}

            };
        }

        private ObservableCollection<Level> _level;
        public ObservableCollection<Level> Levels
        {
            get { return _level; }
            set { _level = value; }
        }
        private Level _slevel;
        public Level SLevel
        {
            get { return _slevel; }
            set { _slevel = value; }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }

    public class Level
    {
        private string _name;
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }
    }
}
