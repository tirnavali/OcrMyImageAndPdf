# Ocr api
## Dependencies
You must install ghostscript to your operating system.
Be sure for ghostscript can be run from command line.
Check the installation with gswin64c.exe command on command shell.

## Contraints
- This application is working only windows machines / servers. 
- All images must be in same format

## Pdf -> Ocr Pdf
Pdf dosyasını türkçe karakter tespiti ile bilgisayar tarafından okunabilir pdf haline getirir.

## Image[] -> Ocr Pdf
Bir veya birden fazla göreseli türkçe karakter tespiti ile bilgisayar tarafından okunabilir pdf haline getirir.
tif, tiff, jpg, jpeg, png, bmp destekler.

## Results 

After processing api return to you a tuple (x,y).
x -> pdf text as a string array. 
y -> pdf as a byte array.

## Bugs
1- Some newspaper pages cannot be read properly.
2- Sağa, sola veya ters dönük sayfalar normal hale getirilip okunuyor. Fakat orjinali pozisyonunda pdf içerisinde basılıyor. Bu sebeple metin ile görüntü hizalama problemi oluşuyor.

## Considered Improvements

- Html parser can be changed to faster one.
- ImgtoPdf is hold images on the ram. So writng them to disk before process will improve the ram usage.