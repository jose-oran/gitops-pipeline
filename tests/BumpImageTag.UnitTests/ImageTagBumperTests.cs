using BumpImageTag;

namespace BumpImageTag.UnitTests;

public class ImageTagBumperTests
{
    private const string SampleKustomization = """
        apiVersion: kustomize.config.k8s.io/v1beta1
        kind: Kustomization
        namespace: gitops-pipeline-dev
        resources:
          - namespace-config.yaml
          - redis.yaml
          - cache-api.yaml
        images:
          - name: ghcr.io/jose-oran/gitops-pipeline-cache-api
            newTag: latest
        """;

    [Fact]
    public void BumpTag_Updates_Only_The_Matching_Images_NewTag_Value()
    {
        var updated = ImageTagBumper.BumpTag(
            SampleKustomization, "ghcr.io/jose-oran/gitops-pipeline-cache-api", "sha-abc1234");

        Assert.Contains("newTag: sha-abc1234", updated);
        Assert.DoesNotContain("newTag: latest", updated);
    }

    [Fact]
    public void BumpTag_Leaves_The_Rest_Of_The_File_Untouched()
    {
        var updated = ImageTagBumper.BumpTag(
            SampleKustomization, "ghcr.io/jose-oran/gitops-pipeline-cache-api", "sha-abc1234");

        Assert.Contains("namespace: gitops-pipeline-dev", updated);
        Assert.Contains("- namespace-config.yaml", updated);
        Assert.Contains("- redis.yaml", updated);
        Assert.Contains("- cache-api.yaml", updated);
        Assert.Contains("name: ghcr.io/jose-oran/gitops-pipeline-cache-api", updated);
    }

    [Fact]
    public void BumpTag_Throws_When_The_Image_Name_Does_Not_Exist()
    {
        Assert.Throws<InvalidOperationException>(
            () => ImageTagBumper.BumpTag(SampleKustomization, "not-an-image", "sha-abc1234"));
    }

    [Fact]
    public void BumpTagInFile_Round_Trips_Through_A_Real_File()
    {
        var path = Path.Combine(Path.GetTempPath(), $"kustomization-{Guid.NewGuid():N}.yml");
        File.WriteAllText(path, SampleKustomization);

        try
        {
            ImageTagBumper.BumpTagInFile(path, "ghcr.io/jose-oran/gitops-pipeline-cache-api", "sha-def5678");
            var afterFirstBump = File.ReadAllText(path);
            Assert.Contains("newTag: sha-def5678", afterFirstBump);

            // Bumping again (as a second deploy would) must still work against the file it
            // just wrote, not just against the original fixture string.
            ImageTagBumper.BumpTagInFile(path, "ghcr.io/jose-oran/gitops-pipeline-cache-api", "sha-9999999");
            var afterSecondBump = File.ReadAllText(path);
            Assert.Contains("newTag: sha-9999999", afterSecondBump);
            Assert.DoesNotContain("sha-def5678", afterSecondBump);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
