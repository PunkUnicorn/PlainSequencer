    param(
		[Parameter()][String]$ImagePath, 
		[Parameter()][Int]$Quality = 90, 
		[Parameter()][Int]$targetSize, 
		[Parameter()][String]$OutputLocation,
		[Parameter()][Int]$OffsetX=0,
		[Parameter()][Int]$OfsetY=0)
 
#PS C:\github\PlainSequencer\lookresults3> & 'C:\Program Files\Google\Chrome\Application\chrome.exe' `
#>> --headless --disable-gpu --screenshot=C:\github\PlainSequencer\lookresults3\screenshot4.png --start-maximized --window-size=2048,1280 `
#>> file:///C:\github\PlainSequencer\lookresults3\index.html
 
 #
 #
 #powershell ..\ResizeImage.ps1 -ImagePath C:\github\PlainSequencer\lookresults3\screenshot4.png -targetSize 1024 -OffsetX -187 -OfsetY -50 -OutputLocation ..\result4.jpg
 #
 #
 #
 
 ### 
 ### S C:\github\PlainSequencer\lookresults3> ..\CropImage.ps1 -ImagePath C:\github\PlainSequencer\result5.jpg -SourceX 188 -SourceY 32 -SourceWidth $(566-188) -SourceHeight $(139-32) -OutputLocation C:\github\PlainSequencer\cropResult5.jpg
 ### 
 
 
 #### ..\TimestampImage.ps1 -ImagePath C:\github\PlainSequencer\cropResult5.jpg -OutputLocation C:\github\PlainSequencer\cropResult5T.jpg
 
 
 
    Add-Type -AssemblyName "System.Drawing"
 
    $img = [System.Drawing.Image]::FromFile($ImagePath)
 
    $CanvasWidth = $targetSize
    $CanvasHeight = $targetSize
 
    #Encoder parameter for image quality
    $ImageEncoder = [System.Drawing.Imaging.Encoder]::Quality
    $encoderParams = New-Object System.Drawing.Imaging.EncoderParameters(1)
    $encoderParams.Param[0] = New-Object System.Drawing.Imaging.EncoderParameter($ImageEncoder, $Quality)
 
    # get codec
    $Codec = [System.Drawing.Imaging.ImageCodecInfo]::GetImageEncoders() | Where {$_.MimeType -eq 'image/jpeg'}
 
    #compute the final ratio to use
    $ratioX = $CanvasWidth / $img.Width;
    $ratioY = $CanvasHeight / $img.Height;
 
    $ratio = $ratioY
    if ($ratioX -le $ratioY) {
        $ratio = $ratioX
    }
 
    $newWidth = [int] ($img.Width * $ratio)
    $newHeight = [int] ($img.Height * $ratio)
 
    $bmpResized = New-Object System.Drawing.Bitmap($newWidth, $newHeight)
    $graph = [System.Drawing.Graphics]::FromImage($bmpResized)
    $graph.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
 
    $graph.Clear([System.Drawing.Color]::White)
    $graph.DrawImage($img, $OffsetX, $OffsetY, $newWidth, $newHeight)
 
    #save to file
    $bmpResized.Save($OutputLocation, $Codec, $($encoderParams))
    $bmpResized.Dispose()
    $img.Dispose()
