inlineHtmlImages

### Inline and Extract base64 image in HTML

Please read [documentation on website](https://landenlabs.com/cs-inlinehtmlimages/inlinehtmlimages.html) for latest notes.


## Author / credits

Author:
  Dennis Lang 2009-2025
  https://landenlabs.com

![](help/InlineHtmlImages.png)

## Description
 

Dennis Lang [https://landenlabs.com/index.html](https://landenlabs.com/index.html)  
Tip publish in [CodePrpoject 20 May 2010](http://www.codeproject.com/Tips/82794/Embedded-HTML-Help-File-with-Images)

Contents:

*   [InlineHtmlImages (auto conversion)](converthtml)
*   [](converthtml)[Manual conversion](#manual)
*   [](#manual)[Warning - base64 limits](#warning)

[](#warning)

* * *

Inlining images in HTML can solve two different problems:

*   Improve page load speed
*   Create a single HTML file that can be embedded into a program.

### What is inlining ?

Inlining refers to replacing an image reference with the image data. Normally HTML displays images using the following syntax:

> <img src='help/happy-image.png'>

The **img** HTML tag tells the browser to load the image from the data source (network or local disk). When an image is inlined or embed the image is placed directly in the HTML in a base64 character encoding. The following HTML syntax is used to inline an image:

> data:\[\]\[;charset=\]\[;base64\],

### Syntax comparison

External image references

Inlined images (xxx...xxx is base64 data)

<img alt="Layout.png" src="help/Layout.png">

<img alt="Layout.png" src="data:image/png;base64,xxx...xxx==">

background-image: url("bg512.png");

background-image: url("data:image/png;base64,xxx....xxx==");

A full description of the data encoding syntax and supported browsers can be found on the following wikis

> *   [http://en.wikipedia.org/wiki/Data\_URI\_scheme#Web\_browser\_support](http://en.wikipedia.org/wiki/Data_URI_scheme#Web_browser_support)
> *   [http://en.wikipedia.org/wiki/Base64](http://en.wikipedia.org/wiki/Base64)

**InlineHtmlImages** is a command line program to automatically convert external image references to inlined base64 character streams. It will inline both standard **<img>** tags and **background-image** styles.

**InlineHtmlImages** has the following main execution modes:

> *   Inline HTML images (default mode)
> *   Extract inlined HTML images
> *   Summary report of HTML images
> *   Base64 conversion of external files.

Here is a sample of **InlineHtmlImages**'s help banner:

> InlineHtmlImages v1.3
> 
> Des:  Convert image references to inlined base64 images 
> Options:
>    -in <inputFile.html>    
>    -out <outputFile.html>  
>    -embed                 ; Encode images into html (default)
>    -extract               ; Extract encoded image from html
>    -summary               ; List image tag info
>    -help                  ; help
> 
>    -toB64                 ; Encode raw input data to base64
>    -fromB64               ; Decode base64 input data
> 
> Examples:
>  ; Inline HTML images referenced in helpOrg.html
>  InlineHtmlImages -embed    -in helpOrg.html -out helpInlined.html
> 
>  ; Reverse inlined HTML and get original images back
>  InlineHtmlImages -extract  -in helpInlined.html -out helpNew.html
> 
>  ; Encode PNG images into base64 character stream
>  InlineHtmlImages -toB64   -in \*.png -out b64\\\*.png
>  InlineHtmlImages -toB64   -in \*.png -out \*.b64
>  InlineHtmlImages -fromB64 -in \*.b64 -out \*.png

As mentioned above, by inlining images a browser will load the page faster because it can avoid multiple trips to the data source. The second reasons is to create a single HTML file. A single HTML file is handy if you want to embed HTML in a program, such as a **Help** document.

[To Top](#top)

* * *

### C# Embedded HTML viewer

An embedded HTML file is one which is built into your program and is not an external file. It is common for program help documentation to be stored in an external .html, .chm or .hlp file. The instant your program has multiple files (executable plus help files), it encourages the author to create an installation process. Under windows you create a .msi or installer. Under unix you make a tar ball. The next problem with a multi-file program is if you want to move the program around or share with a friend, you have to remember to copy all the files and place them in the correct location. To avoid this mess, you can embed HTML help files and have images inlined in the HTML. Thus avoiding installers and sticking with a single program that includes HTML help file with images.

To add an embedded HTML file viewer to your C# program you need to create a webbrowser object which has its document stream set to the embedded HTML file (which has its images inlined).

The .NET webbrowser component supports a data stream which can be an internal embedded resource.

1.  Add your HTML document to your project.
2.  Select its properties and set it as embedded (see example ->)
3.  Set webbrowser document source to embedded resource.

### C# syntax to open browser with embedded html

> Assembly a = Assembly.GetExecutingAssembly();
> Stream HTMLStream = 
>     a.GetManifestResourceStream("<namespace>.<filepath>");	
> webBrowser.DocumentStream = HTMLStream;

### C# Sample code

> namespace AmaZonk
> {
>     public partial class HelpDlg : Form
>     {
>         public HelpDlg()
>         {
>             InitializeComponent();
> 
>             // Attach the embedded html resource
>             Assembly a = Assembly.GetExecutingAssembly();
>             Stream htmlStream = a.GetManifestResourceStream(
> 			    "AmaZonk.Help.AmaZonk.html");
>             this.webBrowser.DocumentStream = htmlStream;
>         }

![](vs-embed-prop.png)

[To Top](#top)

* * *

### Manual Embedding Images in HTML

To produce embedded HTML file in projects, you can manually encode each image into its base64 character stream using an image encoder such as:

*   [http://sourceforge.net/projects/base64/](http://sourceforge.net/projects/base64/)
*   [http://s.codeproject.com/Articles/178661/Converting-Images-to-and-from-Base-Format](http://s.codeproject.com/Articles/178661/Converting-Images-to-and-from-Base-Format)
*   [http://base64.sourceforge.net/](http://base64.sourceforge.net/) or
*   **InlineHtmlImages**
    
    Searched and replaced all of the image tags that look like:
    
    >  <img src="help/image.jpg" alt="image.jpg" />
    
    With the following, where the ...... is the base64 image character stream
    
    >  <img src="data:image/jpg;base64,......." alt="image.jpg" />
    
    I don't know how important it is to include the **/jpg** or what every type of image you have.
    
    >  <img src="data:image**/jpg**;base64,......." alt="image.jpg" />
    
    The reason I say this is I often used the wrong type for my encoded images and the image still displays. When you convert your images to base64 using tool like those referenced above or a website, it will produce a text stream that is either saved in a file or you can redirect into a file. Open the base64 data with your favorite text editor and paste into your HTML file. Here is a sample of a few lines of a base64 encoded image:
    
    > <img src="data:image/png;base64,
    > iVBORw0KGgoAAAANSUhEUgAAAZ8AAAFpCAMAAAB099WiAAAABGdBTUEAALGPC/xhBQAAAwBQTFRF
    > AQEBAAA2NgAANgA2ODgAKysrAABgHjlbADZhHzl3NgBgNzdiAGBgSisQYQAAYAA2YzY2RDlbRDl3
    > YgBiaDlbZzl3SkU/YGA2SEhIRlxbVFNJVlZWTExjQ19vRVt3UVxrS2B5U2FtV2V1Zlp3cGtCa2tr
    > aWJ6anx4eHh4ADaIHzmTNjaIAAD/H1qRH1uuH1ywD26PAGCsL3q8H3nFRDmRYDaHRFqRRFqrTmiO
    > uQmCC" >
    
    The base64 can be spread across multiple lines, as in the example above. The exception is if you add a base64 encoded image to a style sheet. Inside a style sheet, you must remove the line breaks in your base64 image and produce a single super long line.
    
    [](#top> To Top</a>
    <p>
    
    
    <hr class=)
    
    ### WARNING
    
    Your embedded base64 images have to be smallish. I ran into problems with large images not displaying or getting cut in half. I had to reduce the image complexity and shrink its size before encoding to produce a smaller data payload. See [Data URI scheme wiki](http://en.wikipedia.org/wiki/Data_URI_scheme) for details:
    
    > *   Internet Explorer 7 does not support base64 encoded images.
    > *   Internet Explorer 8 can only handle up to 32,000 characters
    > *   C# webbrowser .net v4.0 component has a limit, possibly 32,000 like IE8
    > *   Internet Explorer 9 or newer does not have a limitation.
    > *   Chrome, Firefox, Opera don't have limitations.
    
    Microsoft's discussion on DATA URI limits [url-length-limits-in-internet-explorer](http://blogs.msdn.com/b/ieinternals/archive/2014/08/13/url-length-limits-in-internet-explorer.aspx)
    
    > _Internet Explorer 8 introduced support for DataURIs, a method of encoding a resource directly within a URI using base64 encoding. For performance reasons, IE8 supported DataURIs up to 32kb in length and this longer limit required changes elsewhere. For example, some DOM attributes at the time were limited to 4096 characters, a factor that would have hampered use of DataURIs in the HREF attributes of IMG elements._
    > 
    > _In IE9, the 32kb limit for DataURIs was removed, although for security reasons their use remains limited to certain contexts (specifically, scenarios that create a security context, like IFRAMES, are forbidden). You still should avoid using huge DataURIs in most cases, however, as performance of large DataURIs often lags other approaches._
    
    [To Top](#top)

[Top](#top)
