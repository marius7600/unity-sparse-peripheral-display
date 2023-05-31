using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightProbeRenderer : MonoBehaviour
{
    public LightProbe[] lightProbes; // Array der Light Probes

    public Camera renderCamera; // Referenz auf die MainCamera
    private Texture2D cameraTexture; // Textur, um das gerenderte Bild der MainCamera zu speichern
    private int[] nearestProbeIndices; // Zuordnung der Pixel zu den Light Probes

    void Start()
    {
        // Skaliere die RenderTexture der MainCamera auf 1/4 der Größe
        renderCamera.targetTexture = new RenderTexture(renderCamera.pixelWidth / 4, renderCamera.pixelHeight / 4, 24);

        // Erstelle die Textur, um das gerenderte Bild der MainCamera zu speichern
        cameraTexture = new Texture2D(renderCamera.pixelWidth, renderCamera.pixelHeight);

        // Berechne das Voronoi-Diagramm einmalig
        CalculateVoronoiDiagram();
    }

    void LateUpdate()
    {
        // Aktualisiere die Light Probes basierend auf dem vorberechneten Voronoi-Diagramm
        StartCoroutine(UpdateLightProbes());

        // // alle 10 Frames aktualisieren
        // if (Time.frameCount % 10 == 0)
        // {
        //     StartCoroutine(UpdateLightProbes());
        // }
    }

    void CalculateVoronoiDiagram()
    {
        // Erstelle das Voronoi-Diagramm und berechne Durchschnittsfarbwerte
        nearestProbeIndices = new int[cameraTexture.width * cameraTexture.height];
        Color[] averageColors = new Color[lightProbes.Length];
        int[] probeCounts = new int[lightProbes.Length];

        for (int i = 0; i < cameraTexture.width; i++)
        {
            for (int j = 0; j < cameraTexture.height; j++)
            {
                Vector2 pixelPosition = new Vector2(i, j);
                nearestProbeIndices[j * cameraTexture.width + i] = FindNearestProbeIndex(pixelPosition);
            }
        }

        for (int i = 0; i < cameraTexture.width; i++)
        {
            for (int j = 0; j < cameraTexture.height; j++)
            {
                int pixelIndex = j * cameraTexture.width + i;
                int nearestProbeIndex = nearestProbeIndices[pixelIndex];

                averageColors[nearestProbeIndex] += cameraTexture.GetPixel(i, j);
                probeCounts[nearestProbeIndex]++;
            }
        }

        for (int i = 0; i < lightProbes.Length; i++)
        {
            if (probeCounts[i] > 0)
            {
                averageColors[i] /= probeCounts[i];
                lightProbes[i].UpdateProbeColor(averageColors[i]);
            }
        }
    }

    IEnumerator UpdateLightProbes()
    {
        yield return new WaitForEndOfFrame();

        // Rendere das Bild der MainCamera in die Textur
        RenderTexture currentRT = RenderTexture.active;
        RenderTexture.active = renderCamera.targetTexture;
        cameraTexture.ReadPixels(new Rect(0, 0, cameraTexture.width, cameraTexture.height), 0, 0);
        cameraTexture.Apply();
        RenderTexture.active = currentRT;

        // Weise den Light Probes die Farben basierend auf dem Voronoi-Diagramm zu
        Color[] averageColors = new Color[lightProbes.Length];
        int[] probeCounts = new int[lightProbes.Length];

        // Erhalte die Pixelwerte der Kameratextur
        Color[] pixels = cameraTexture.GetPixels();

        // ---- Berchnung der Durchschnittsfarbwerte in Batches ----
        // int batchSize = 200;
        // int numBatches = Mathf.CeilToInt((float)pixels.Length / batchSize);

        // for (int batchIndex = 0; batchIndex < numBatches; batchIndex++)
        // {
        //     int startIndex = batchIndex * batchSize;
        //     int endIndex = Mathf.Min(startIndex + batchSize, pixels.Length);

        //     for (int i = startIndex; i < endIndex; i++)
        //     {
        //         int nearestProbeIndex = nearestProbeIndices[i];

        //         averageColors[nearestProbeIndex] += pixels[i];
        //         probeCounts[nearestProbeIndex]++;
        //     }
        // }
        // ----


        // ---- Berchnung der Durchschnittsfarbwerte ohne Batches ----
        // Iteriere über die Pixel und aktualisiere die Durchschnittsfarbwerte
        for (int i = 0; i < pixels.Length; i++)
        {
            int nearestProbeIndex = nearestProbeIndices[i];

            averageColors[nearestProbeIndex] += pixels[i];
            probeCounts[nearestProbeIndex]++;
        }
        // ----

        // Debug.Log("Width: " + cameraTexture.width + ", Height: " + cameraTexture.height + ", Total Pixels:" + pixels.Length);

        // Aktualisiere die Light Probes mit den Durchschnittsfarbwerten
        for (int i = 0; i < lightProbes.Length; i++)
        {
            if (probeCounts[i] > 0)
            {
                averageColors[i] /= probeCounts[i];
                lightProbes[i].UpdateProbeColor(averageColors[i]);
            }
        }
    }


    int FindNearestProbeIndex(Vector2 position)
    {
        int nearestIndex = 0;
        float nearestDistance = Mathf.Infinity;

        for (int i = 0; i < lightProbes.Length; i++)
        {
            float distance = Vector2.Distance(position, renderCamera.WorldToScreenPoint(lightProbes[i].GetProbePosition()));
            if (distance < nearestDistance)
            {
                nearestIndex = i;
                nearestDistance = distance;
            }
        }

        return nearestIndex;
    }
}
