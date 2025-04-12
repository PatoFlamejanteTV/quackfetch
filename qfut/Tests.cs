using System;
using NUnit.Framework;


namespace qfut
{
    [TestFixture]
    public class MachineInfoTests
    {
        [Test]
        public void GetMachineId_ReturnsNonEmptyString()
        {
            // Act
            string machineId = MachineInfo.GetMachineId();
            
            // Assert
            Assert.IsNotNull(machineId);
            Assert.IsNotEmpty(machineId);
            Assert.AreNotEqual("Unknown Device", machineId, "The machine ID should not be unknown.");
        }
        
        [Test]
        public void CleanModelString_RemovesUnwantedStrings()
        {
            // Arrange
            string dirtyModel = "System Product Name To be filled by O.E.M.";
            
            // Act
            string cleanedModel = MachineInfo.CleanModelString(dirtyModel);
            
            // Assert
            Assert.AreNotEqual(dirtyModel, cleanedModel);
            Assert.IsFalse(cleanedModel.Contains("To be filled by O.E.M."));
            Assert.IsFalse(cleanedModel.Contains("System Product Name"));
        }
        
        [Test]
        public void GetPrettyName_ReturnsOperatingSystemName()
        {
            // Act
            string osName = MachineInfo.GetPrettyName();
            
            // Assert
            Assert.IsNotNull(osName);
            Assert.IsNotEmpty(osName);
        }
        
        [Test]
        public void GetKernel_ReturnsKernelVersion()
        {
            // Act
            string kernel = MachineInfo.GetKernel();
            
            // Assert
            Assert.IsNotNull(kernel);
            Assert.IsNotEmpty(kernel);
        }
        
        [Test]
        public void ExecuteShellCommand_WithValidCommand_ReturnsOutput()
        {
            // Arrange
            string command = "echo 'teste'";
            
            // Act
            string result = MachineInfo.ExecuteShellCommand(command);
            
            // Assert
            Assert.IsNotNull(result);
            StringAssert.Contains("teste", result);
        }
        
        [Test]
        public void GetPackages_ReturnsPackageCount()
        {
            // Act
            string packages = MachineInfo.GetPackages();
            
            // Assert
            Assert.IsNotNull(packages);
            Assert.IsNotEmpty(packages);
        }
    }
}