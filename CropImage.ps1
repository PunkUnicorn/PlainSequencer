    param(
		[Parameter()][String]$ImagePath, 
		[Parameter()][Float]$SourceX=0, 
		[Parameter()][Float]$SourceY=0,
		[Parameter()][Float]$SourceWidth=0,
		[Parameter()][Float]$SourceHeight=0,
		[Parameter()][Int]$Quality = 90, 		
		[Parameter()][String]$OutputLocation)
 
    Add-Type -AssemblyName "System.Drawing"
 
    $img = [System.Drawing.Image]::FromFile($ImagePath)
 
    $CanvasWidth = $SourceWidth
    $CanvasHeight = $SourceHeight
 
    #Encoder parameter for image quality
    $ImageEncoder = [System.Drawing.Imaging.Encoder]::Quality
    $encoderParams = New-Object System.Drawing.Imaging.EncoderParameters(1)
    $encoderParams.Param[0] = New-Object System.Drawing.Imaging.EncoderParameter($ImageEncoder, $Quality)
 
    # get codec
    $Codec = [System.Drawing.Imaging.ImageCodecInfo]::GetImageEncoders() | Where {$_.MimeType -eq 'image/jpeg'}
 
    # #compute the final ratio to use
    # $ratioX = $CanvasWidth / $img.Width;
    # $ratioY = $CanvasHeight / $img.Height;
 
    # $ratio = $ratioY
    # if ($ratioX -le $ratioY) {
        # $ratio = $ratioX
    # }
 
    $newWidth = [int]$SourceWidth #[int] ($img.Width * $ratio)
    $newHeight = [int]$SourceHeight #[int] ($img.Height * $ratio)
 
    $bmpResized = New-Object System.Drawing.Bitmap($newWidth, $newHeight)
    $graph = [System.Drawing.Graphics]::FromImage($bmpResized)
    #$graph.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
 
	$section = New-Object System.Drawing.Rectangle($SourceX, $SourceY, $SourceWidth, $SourceHeight)
	#// Create image attributes and set large gamma.
    $imageAttr = New-Object System.Drawing.Imaging.ImageAttributes
    #imageAttr.SetGamma(4.0F);
	
    $graph.Clear([System.Drawing.Color]::White)
	$CroppedImage = $img.Clone($section, $img.PixelFormat);	
	#$graph.DrawImage($img, $destRect2, $SourceX, $SourceY, $SourceWidth, $SourceHeight, [System.Drawing.GraphicsUnit]::Pixel, $imageAttr)
	###$graph.DrawImage($img, 0, 0, $section, [System.Drawing.GraphicsUnit]::Pixel);
    #$graph.DrawImage($img, $OffsetX, $OffsetY, $newWidth, $newHeight)
 
    #save to file
    $CroppedImage.Save($OutputLocation, $Codec, $($encoderParams))
    $CroppedImage.Dispose()
    $img.Dispose()
