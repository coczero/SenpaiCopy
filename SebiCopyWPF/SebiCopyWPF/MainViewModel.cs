﻿using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Linq;

namespace SebiCopyWPF
{
  /// <summary>
  /// ViewModel for the MainView.
  /// </summary>
  class MainViewModel : PropertyChangedBase
  {
    private List<string> _pathList;
    private ObservableCollection<PathCheckBox> _checkBoxList;
    private int _currentImageIndex;
    private ImageSource _currentImage;
    private bool _includeSubDirectories;

    private string _imagePath;
    private string _folderPath;

    private bool _deleteImage;

    private PathCheckBox _currentRightClickedCheckBox;

    /// <summary>
    /// Gets/sets the currently shown image.
    /// </summary>
    public ImageSource CurrentImage
    {
      get { return _currentImage; }
      private set
      {
        _currentImage = value;
        NotifyOfPropertyChange(() => CurrentImage);
        NotifyOfPropertyChange(() => CanCopy);
      }
    }

    /// <summary>
    /// Gets a list of all PathCheckBoxes
    /// </summary>
    public ObservableCollection<PathCheckBox> CheckBoxList
    {
      get { return _checkBoxList; }
      private set { _checkBoxList = value; }
    }

    /// <summary>
    /// Gets/sets wether subdirectories get included in select folder path.
    /// </summary>
    public bool IncludeSubDirectories
    {
      get { return _includeSubDirectories; }
      set { _includeSubDirectories = value; }
    }

    /// <summary>
    /// The path to the folder with the images.
    /// </summary>
    public string ImagePath
    {
      get { return _imagePath; }
      private set
      {
        _imagePath = value;
        NotifyOfPropertyChange(() => ImagePath);
      }
    }

    /// <summary>
    /// The path to the folder of the folders.
    /// </summary>
    public string FolderPath
    {
      get { return _folderPath; }
      private set
      {
        _folderPath = value;
        NotifyOfPropertyChange(() => FolderPath);
      }
    }

    /// <summary>
    /// Gets/sets wether the image should be deleted after copying.
    /// </summary>
    public bool DeleteImage
    {
      get { return _deleteImage; }
      set 
      { 
        _deleteImage = value;
        NotifyOfPropertyChange(() => CanCopy);
      }
    }

    /// <summary>
    /// Gets if the previous button is enabled.
    /// </summary>
    public bool CanPrevious
    {
      get { return _currentImageIndex != 0; }
    }

    /// <summary>
    /// Gets if the copy button is enabled.
    /// </summary>
    public bool CanCopy
    {
      get { return CurrentImage != null && (DeleteImage || CheckBoxList.Count(i => (bool)i.IsChecked) != 0); }
    }

    /// <summary>
    /// Gets if the next button is enabled.
    /// </summary>
    public bool CanNext
    {
      get { return _pathList.Count - 1 > _currentImageIndex; }
    }

    /// <summary>
    /// Ctor.
    /// </summary>
    public MainViewModel()
    {
      _pathList = new List<string>();
      _checkBoxList = new ObservableCollection<PathCheckBox>();
      IncludeSubDirectories = true;
      DeleteImage = true;
    }

    /// <summary>
    /// Lets the user select a folder with images and loads them.
    /// </summary>
    public void SelectImageFolder()
    {
      FolderBrowserDialog dlg = new FolderBrowserDialog();
      if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
      {
        ImagePath = dlg.SelectedPath;

        //get all files
        string[] files = Directory.GetFiles(dlg.SelectedPath);

        //get all image files
        foreach (string file in files)
        {
          string test = file.Substring(file.Length - 4, 4);
          if (file.Substring(file.Length - 4, 4) == ".png" || file.Substring(file.Length - 4, 4) == ".bmp" || file.Substring(file.Length - 4, 4) == ".jpg" || file.Substring(file.Length - 5, 5) == ".jpeg" || file.Substring(file.Length - 4, 4) == ".gif")
          {
            _pathList.Add(file);
          }
        }
      }

      UpdatePictureBox();
    }

    /// <summary>
    /// Opens a dialog to select a subfolder and creates checkBoxes for each path.
    /// </summary>
    public void SelectFolderPath()
    {
      FolderBrowserDialog dlg = new FolderBrowserDialog();
      if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
      {
        CheckBoxList.Clear();

        FolderPath = dlg.SelectedPath;

        SearchOption so;
        if ((bool)IncludeSubDirectories)
          so = SearchOption.AllDirectories;
        else
          so = SearchOption.TopDirectoryOnly;

        string[] subfolders = Directory.GetDirectories(dlg.SelectedPath, "*", so);

        List<PathCheckBox> _checkBoxes = new List<PathCheckBox>();
        foreach (string folder in subfolders)
        {
          PathCheckBox chk = new PathCheckBox();
          chk.Content = new DirectoryInfo(folder).Name;
          chk.FullPath = folder;
          chk.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
          chk.VerticalAlignment = System.Windows.VerticalAlignment.Stretch;
          chk.Margin = new Thickness(10, 0, 0, 0);
          chk.MouseRightButtonDown += new System.Windows.Input.MouseButtonEventHandler(CheckBox_RightMouseDown);
          CheckBoxList.Add(chk);
        }
      }
    }

    /// <summary>
    /// Updates the currently shown image.
    /// </summary>
    private void UpdatePictureBox()
    {
      if (_pathList.Count > _currentImageIndex)
      {
        try
        {
          CurrentImage = LoadBitmapImage(_pathList[_currentImageIndex]);
        }
        catch (OutOfMemoryException)
        {
          System.Windows.Forms.MessageBox.Show("The RAM is a lie. Folgendes Bild hat den Error verursacht: " + _pathList[_currentImageIndex] + " | Wir skippen das Bild einfach mal.");
          if (CurrentImage != null) // TODO: check if still needed.
            CurrentImage = null;

          _currentImageIndex++;
          UpdatePictureBox();
        }
      }
      else // no more images.
      {
        CurrentImage = null;
        _currentImageIndex = 0;
      }

      NotifyOfPropertyChange(() => CanPrevious);
      NotifyOfPropertyChange(() => CanNext);
    }

    /// <summary>
    /// Copies the current image to the selected folders.
    /// </summary>
    public void Execute()
    {
      List<string> dirsToCopyTo = new List<string>();

      foreach (PathCheckBox c in CheckBoxList)
      {
        if ((bool)c.IsChecked)
          dirsToCopyTo.Add(c.FullPath);
      }

      foreach (string dir in dirsToCopyTo)
      {
        File.Copy(_pathList[_currentImageIndex], dir + @"\" + System.IO.Path.GetFileName(_pathList[_currentImageIndex]), true);
      }

      if (DeleteImage)
        File.Delete(_pathList[_currentImageIndex]);

      foreach (PathCheckBox c in CheckBoxList)
      {
        c.IsChecked = false;
      }

      _currentImageIndex++;
      UpdatePictureBox();
    }

    /// <summary>
    /// Skips the current image doing nothing with it.
    /// </summary>
    public void Next()
    {
      _currentImageIndex++;
      UpdatePictureBox();
    }

    /// <summary>
    /// Goes to the previous image.
    /// </summary>
    public void Previous()
    {
      _currentImageIndex--;
      UpdatePictureBox();
    }

    /// <summary>
    /// Opens the current image in the standard image viewer.
    /// </summary>
    public void ShowImage()
    {
      Process.Start((CurrentImage as BitmapImage).UriSource.LocalPath);
    }

    /// <summary>
    /// Removes the selected checkBox.
    /// </summary>
    public void RemoveCheckBox()
    {
      CheckBoxList.Remove(_currentRightClickedCheckBox);
    }

    /// <summary>
    /// Loads an image file from the given <paramref name="fileName"/>.
    /// This does still allow operations done to the file.
    /// </summary>
    /// <param name="fileName">Image file to load.</param>
    /// <returns>Loaded image file.</returns>
    private static BitmapImage LoadBitmapImage(string fileName)
    {
      var bitmapImage = new BitmapImage();
      bitmapImage.BeginInit();
      bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
      bitmapImage.UriSource = new Uri(fileName);
      bitmapImage.EndInit();
      bitmapImage.Freeze();
      return bitmapImage;
    }

    /// <summary>
    /// Sets the current right clicked checkBox.
    /// </summary>
    private void CheckBox_RightMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
      _currentRightClickedCheckBox = sender as PathCheckBox;
    }
  }
}