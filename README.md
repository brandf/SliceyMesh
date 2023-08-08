# SliceyMesh

Slicey Mesh is an open source Unity package (MIT license).  The purpose of the package is to dynamically generate parametric 3d shapes.  With an emphasis on 3d shapes with nice rounded edges.

![Slicey Mesh Dark UI](Docs/SliceyMeshDark.gif?raw=true "Slicey Mesh Dark UI")

# Features
* Dynamically generate meshes for a variety of  parametrically sized shapes
* Support edge/corner radii
* Focus on high performance and low memory
  - Extensive caching
  - Dynamic LOD
  - Performance testing
  - Instancing & Batching friendly
  - Utililze CPU and GPU


## What does it mean to be parametric?

![Slicey Mesh Editor](Docs/sliceymesheditor.gif?raw=true "Slicey Mesh Editor")

Non-parametric shapes are basically static 3d models.  You could of course generate all of these shapes in your favorite 3d modeling software, but then you can't change/animate the size, corner radii, etc. at runtime.  With a parametric shapes you can use the inspector or code to make changes to these things at runtime.

The shapes Slicey Mesh is capable of generating is more extensive than with what comes with Unity out of the box, mainly because it supports rounded edges and fillets.  As soon as you have these things, the difference between size and scale becomes apparent.  It's common practice to makes 1x2x3 meter cube in Unity by scaling it, but you really don't want your rounded edges stretched like that!  With Slicey Mesh you can parametrically change while preserving an edge radius, or vice versa.

## What is a fillet?

![Slicey Mesh Boxes](Docs/Boxes.png?raw=true "Slicey Mesh Boxes")

In CAD terms, a fillet is where you round off an edge.  This is desirable for 3d shape geometry because the filleted edges provide better specular highlights and create softer shapes.  In the images of cubes and cylinders below, compare the non-filleted shapes on the right, with the slightly-filleted versions on to their left.  Notice how much better the inner-edges pop with fillets?  With Slicey Mesh you can independently control multiple radii to get the desired look.

![Slicey Mesh Cylinders](Docs/Cylinders.png?raw=true "Slicey Mesh Cylinders")

Slicey Mesh supports adjustable "quality" settings as well as automatic LOD on all shapes that have curved surfaces.

## Why not use SDF-like shaders similar to Shapes?

![Slicey Mesh OddShapes](Docs/OddShapes.png?raw=true "Slicey Mesh OddShapes")

Slicey Mesh generates triangles/geometry, even for 2d shapes like rounded rectangles.  There are pros/cons to both approaches, but Slicey Mesh is specifically designed for generating meshes.  The reasons for this include:

1) Meshes work better with MSAA - doing AA on SDF shapes requires alpha blending, which causes seaming issues.
2) Meshes work better with the Z-buffer - SDF approaches do not benefit from a variety of hardware optimizations that modern GPUs provide for Z-culling.
3) Meshes are overall less expensive - SDF approaches can be fast for simple 2d shapes, but for complex 3d shapes they end up looking like ray-tracing inside the pixel shader, which can be more expensive than the triangle-rasterizing alternatives.  With mesh generation, much of the cost is paid once at loading time, and the results can be cached.

## What is Slicing?

![Slicey Mesh 9 grid](Docs/9grid.png?raw=true "Slicey Mesh 9 grid")

Dynamically generating meshes can be expensive, however Slicey Mesh uses a number of techniques which improve performance.  The best way to go fast is to not do something.  Rather than generating new meshes for every parametric change, Slicey Mesh generates/caches 'canonical' shapes and then "slices" these shapes into the final size/radii (potentially also cached).

The slicing process is conceptually similar to the common technique of 9-slice scaling done on 2d images or the Lattice modifier in Blender.  Effectively, the vertices of the mesh are morphed from the canonical mesh to the desired mesh using either the CPU or the GPU with a compute or vertex shader.

## What materials are supported?

![Slicey Mesh Triplaner](Docs/triplaner.gif?raw=true "Slicey Mesh Triplaner")

Slicey Mesh works with any Unity material, however the meshes that are generated only have position & normal attributes.  Optionally, you can indicate that your material supports shader based slicing, in which case the caching system has greater freedom, and this can result in better performance.

## What is it used for?

![Slicey Mesh UI](Docs/UI.png?raw=true "Slicey Mesh UI")

Slicey Mesh is a 'foundation' package, with a clear scope and no dependencies.  It generates meshes and that's about it.  To use it, you would typically combine it with other systems.  For example, it can be used to visualize 3D UI, to mock out world geometry (think ProBuilder), or to produce interesting visual affordances.
