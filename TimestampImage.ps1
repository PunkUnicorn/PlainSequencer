    param(
		[Parameter()][String]$ImagePath, 
		[Parameter()][Int]$Quality = 90, 
		[Parameter()][String]$OutputLocation)
 
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
 
    $newWidth = $img.Width
    $newHeight = $img.Height
 
    $bmpResized = New-Object System.Drawing.Bitmap($newWidth, $newHeight)
    $graph = [System.Drawing.Graphics]::FromImage($bmpResized)
    $graph.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
 
    $graph.Clear([System.Drawing.Color]::White)
    $graph.DrawImage($img, 0, 0, $newWidth, $newHeight)
	
  #// Create string to draw.
    $drawString = (Get-Date).DateTime;
             
    #// Create font and brush.
    $drawFont = New-Object System.Drawing.Font("Arial", 7)
    #$drawBrush = New-Object System.Drawing.SolidBrush [System.Drawing.Color]::Color.DimGray
    $drawBrush = new-object Drawing.SolidBrush LightGray

	$blackPen = new-object Drawing.Pen LightGray
   
    $section = New-Object System.Drawing.Rectangle(0, 0, $($newWidth/4), $($newHeight/4))
         
    #// Draw rectangle to screen.
    #$blackPen = New-Object System.Drawing.Rectangle.Pen System.Drawing.Color.DimGray
    #$graph.DrawRectangle $blackPen, 0, 0, width, height);
             
    #// Set format of string.
    $drawFormat = New-Object System.Drawing.StringFormat
    #$drawFormat.Alignment = [System.Drawing.StringFormat.StringAlignment]::Center
             
    #// Draw string to screen.
    $graph.DrawString($drawString, $drawFont, $drawBrush, 0.1, 0.1)
	
    #save to file
    $bmpResized.Save($OutputLocation, $Codec, $($encoderParams))
    $bmpResized.Dispose()
    $img.Dispose()
