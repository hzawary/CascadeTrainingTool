# CascadeTrainingTool
An useful windows application as user interface to cascade classifier training using OpenCV applications.
[![CascadeTrainingBuilder](http://s6.picofile.com/file/8223404942/CascadeTrainingBuilderTool.jpg)](http://www.namasha.com/v/RcPZJdX2)

Base program developed using Visual Studio 2015, C# .Net 4.6, compiled in x86 windows 10.

By References:
- http://note.sonots.com/SciSoftware/haartraining.html
- http://docs.opencv.org/2.4/doc/user_guide/ug_traincascade.html
- http://www.technolabsz.com/2011/08/how-to-do-opencv-haar-training.html
- http://coding-robin.de/2013/07/22/train-your-own-opencv-haar-classifier.html
- http://www.technolabsz.com/2012/07/how-to-do-opencv-haar-training-in.html
- http://www.prodigyproductionsllc.com/articles/programming/how-to-train-opencv-haar-classifiers/

## **Requiring before runnig the program**
- Adding complied OpenCV apps in bin project directory. In this project used **_opencv_annotation.exe_**, **_opencv_createsamples.exe_**, **_opencv_ffmpeg300.dll_**, **_opencv_traincascade.exe_**, **_opencv_world300.dll_**, **_opencv_world300d.dll_** files about 18 MB (OpenCV v3.0).
- Mentioned files can be download from [here](https://github.com/hzawary/CascadeTrainingTool/releases).

### Short keys
- **_Ctrl+Right_** and **_Ctrl+Left_** for goto next image and previous image respectively.
- **_Ctrl+Up_** and **_Ctrl+Down_** for accept image as positive and negative respectively and goto the next image.

### The result program structure
- **_Negatives_**: Directory include a text file and negative images.
- **_Positives_**: Directory include a text file and positive images.
- **_samples.vec_**: Vector file from positive images.
- **_Cascade_** _ **_??x??_**: Directory (an arbitrary name) include trained xml files from vector file and negative images.
