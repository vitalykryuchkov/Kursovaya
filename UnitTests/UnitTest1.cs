using System.Drawing;
using SixLabors.ImageSharp.PixelFormats;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.ColorSpaces;

namespace UnitTests;

[TestClass]
public class UnitTest1
{
    [TestMethod]
    public async Task TestGenerateASCIIArt()
    {
        //Arrange
        var img = new Image<Rgba32>(10,10);

        for (int y = 0; y < img.Height; y++) {
            for (int x = 0; x < img.Width; x++) {
                img[x,y] = new Rgba32(0, 0, 0);
            }
        }

        //Act
        string result = await ASCIIArtCreator.GenerateASCIIArt(img);

        //Assert
        string expectedOutput = "@@@@@@@@@@\n@@@@@@@@@@\n@@@@@@@@@@\n@@@@@@@@@@\n@@@@@@@@@@\n";
        Assert.AreEqual(expectedOutput, result);
    }

    [TestMethod]
    public async Task TestImageHeightAndWeight() 
    {
        //Arrange
        var img = new Image<Rgba32>(1, 1);

        //Act
        string result = await ASCIIArtCreator.GenerateASCIIArt(img);

        //Assert
        string expectedOutput = "Изображение слишком мало для генерации ASCII art.";
        Assert.AreEqual(expectedOutput, result);
    }

    [TestMethod]
    public void TestDeleteFile() 
    {
        //Arrange
        string filepath = "testfile.txt";
        File.WriteAllText(filepath, "Test content");

        //Act
        string deletionResult = ASCIIArtCreator.DeleteFile(filepath);

        //Assert
        Assert.AreEqual("Файл успешно удален", deletionResult);
        Assert.IsFalse(File.Exists(filepath));
    }

    [TestMethod]
    public void TestDeleteFile_FileNotFound() 
    {
        //Arrange
        string filepath = "nonexistentfile.txt";

        //Act
        string result = ASCIIArtCreator.DeleteFile(filepath);

        //Assert
        Assert.AreEqual("Файл не найден", result);
    }

    [TestMethod]
    public void TestDeleteFile_EmptyPath() 
    {
        //Act
        string result = ASCIIArtCreator.DeleteFile("");

        //Assert
        Assert.AreEqual("Имя файла не может быть пустым", result);
    }
}