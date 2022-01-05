
This was only meant as a test for particle snow works and as such is free to use.
(Also the reason as to why comments are far and few inbetween)

Some notes if you planning to use this.

REQUIREMENTS:
URP (Shader), VFX graph (https://docs.unity3d.com/Packages/com.unity.visualeffectgraph@6.7/manual/index.html) 
Do not forget to create a RT_Particle layer and assign it to the RT_Painter, also be sure to cull them in the correct cameras


*The mesh generation can be subdivided to get a higher resolution mesh but i was to lazy to implement it.

*The shader can be remade to skip textures and work with noise instead which is generally a better idea.

*Currently there is only support for 1 RT camera but it should be easy to add support for more if needed.

