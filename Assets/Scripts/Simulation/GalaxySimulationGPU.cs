using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using Simulation.Struct;


namespace Simulation
{


    public class GalaxySimulationGPU : MonoBehaviour
    {
        bool simulationStarted = false;
        [SerializeField] ComputeShader computeShader;

        private int kernel;
        [SerializeField]
        private ComputeBuffer starsBuffer;

        int starCount;

        [SerializeField]
        private Material renderMaterial;

        private bool render = false;
        SimulationParameter simulationParameter;

        // Start is called before the first frame update
        public void Spawn()
        {
            Delete();

            simulationParameter = GlobalManager.Instance.SimulationParameter;


            float galaxyRadius = simulationParameter.Radius;
            float galaxyThickness = simulationParameter.Thickness;
            float starInitialVelocity = simulationParameter.InitialVelocity;
            starCount = (int)simulationParameter.BodiesCount;
            float smoothingLenght = simulationParameter.SmoothingLength;
            float blackHoleMass = simulationParameter.BlackHoleMass;
            float interactionRate = simulationParameter.InteractionRate;

            SetupShader(starCount);
            InitStarsAttribute(starCount, Time.deltaTime, smoothingLenght, interactionRate, blackHoleMass);
            InitStarsPos(simulationParameter);
            renderMaterial.SetInt("simulationType", (int)simulationParameter.simulationType);
            simulationStarted = true;
            render = true;

        }

        void Update()
        {
            if (simulationStarted)
            {
                RunComputeShader();
            }
        }
        void RunComputeShader()
        {
            computeShader.Dispatch(kernel, starCount / 128 + 1, 1, 1);
        }

        public void SetupShader(int n)
        {
            try
            {
                kernel = computeShader.FindKernel("UpdateStars");
            }
            catch
            {
                Debug.Log("kernel not found");
            }
            int bufferStride = sizeof(float) * 6;
            starsBuffer = new ComputeBuffer(starCount, bufferStride);
            computeShader.SetBuffer(kernel, "star", starsBuffer);

        }

        public void InitStarsAttribute(int n, float deltaTime, float smoothingLenght, float interactionRate, float blackHoleMass)
        {
            computeShader.SetInt("starCount", n);
            computeShader.SetFloat("deltaTime", deltaTime);
            computeShader.SetFloat("smoothingLenght", smoothingLenght);
            computeShader.SetFloat("interactionRate", interactionRate);
            computeShader.SetFloat("blackHoleMass", blackHoleMass);


        }

        public void InitStarsPos(SimulationParameter parameter)
        {
            starsBuffer.SetData(BodiesInitializerFactory.Create(parameter).InitStars());
        }


        private void OnRenderObject()
        {
            if (render)
            {
                renderMaterial.SetBuffer("starBuffer", starsBuffer);
                // Render the stars
                renderMaterial.SetPass(0);
                Graphics.DrawProceduralNow(MeshTopology.Points, starCount);
            }
        }

        public void Delete()
        {
            if (starsBuffer != null)
                starsBuffer.Dispose();
            simulationStarted = false;
            render = false;
        }

    }
}