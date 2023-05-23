<script lang="ts" context="module">
    type Circle = {row:number, col:number};
    type SVGDot = {x:number, y:number, color?:string};
    type SVGStroke = {color?:string, d:string}
</script>
<script lang="ts">
    // We need a list of dots and lines, i.e. which dots go to which parents
    // Each dot has one or two parents, and needs a Bezier curve to the parent
    export let paths: [number, number][] = [];  // Format is [0,1],[1,2],[2,3],[2,4]. Dot 0 connects to dot 1. Dot 1 connects to dot 2. Dot 2 connects to both both 3 and 4, but they are separate paths.
    export let circles: Circle[] = [];  // Format is [{row:0,col:0},{row:1,col:0},{row:2,col:0},{row:2,col:1}]. Each dot has a row and column, which are auto-translated to x and y
    export let rowHeights: number[] = [];

    export let firstRowOffset = 10;
    export let firstColOffset = 10;
    export let colWidthDefault = 20; // May be auto-calculated in the future
    export let rowHeightDefault = 20;
    export let circleSize = 4;

    export let colors = [ // Default set of colors works nicely, but allow overriding if needed
        "#4e79a7",
        "#f28e2c",
        "#e15759",
        "#76b7b2",
        "#59a14f",
        "#edc949",
        "#af7aa1",
        "#ff9da7",
        "#9c755f",
        "#bab0ab",
    ];
    $: colorLength = colors.length;
    function color(colIdx: number) {
        return colors[colIdx % colorLength];
    }

    function calculateCumulativeHeights(simpleHeights: number[]) {
        let firstRow = (simpleHeights && simpleHeights.length > 0) ? (simpleHeights[0] / 2) : firstRowOffset;
        let total = firstRow;
        let result = [firstRow];
        for (const height of simpleHeights) {
            total += height;
            result.push(total);
        }
        return result;
    }
    $: cumulativeHeights = calculateCumulativeHeights(rowHeights);
    
    function rowHeight(rowIdx: number) {
        return cumulativeHeights[rowIdx] ? cumulativeHeights[rowIdx] : rowHeightDefault * rowIdx + firstRowOffset;
    }

    function colWidth(colIdx: number) {
        return colWidthDefault * colIdx + firstColOffset;
    }

    function bezier(from: Circle, to: Circle) {
        /* 
        - If parent was in same column, M (child X,Y) and then V (parent Y)
        - If no parent, vertical line to bottom of graph (same as above but V (bottom-of-graph Y) instead of parent Y)
        - If parent was in different column, Bezier curve as follows:
            - Calculate halfway-point between parent and child. Call it Hx, Hy. Cx, Cy is child, and Px, Py is parent.
            - M child X,Y
            - C Cx,Cy to Cx,Hy to Hx,Hy
            - C Hx,Hy to Px,Hy to Px,Py
            - Note that parents are *below* children in this graph, so Hy is below Cy and above Py
            - If I understand that correctly, that could become:
            - M Cx, Cy
            - S Cx,Hy Hx,Hy
            - S Px,Hy Px,Py
        */
        if (from && to) {  // TODO: Fix this hack once I figure out why the "to" is sometimes undefined
        let { row: fromRow, col: fromCol } = from;
        let { row: toRow, col: toCol } = to;
        let strokeColor = color(fromCol);  // Consider making it toCol instead
        let fromX = colWidth(fromCol);
        let toX = colWidth(toCol);
        let fromY = rowHeight(fromRow);
        let toY = rowHeight(toRow);
        if (fromX == toX) {
            return { color: strokeColor, d: `M${fromX} ${fromY}V${toY}` };
        } else {
            let halfX = (fromX + toX) / 2;
            let halfY = (fromY + toY) / 2;
            return { color: strokeColor, d: `M${fromX} ${fromY}S${fromX} ${halfY},${halfX} ${halfY}S${toX} ${halfY},${toX} ${toY}` };
        }
    } else return { color: '#000000', d: ''}
    }

    let curves: SVGStroke[] = [];
    let svgCircles: SVGDot[] = [];

    $: {
        cumulativeHeights;  // Lets Svelte know about the hidden dependency on this array in bezier()
        curves = paths.map(([f,t]) => bezier(circles[f], circles[t]));
        svgCircles = circles.map(({ row, col }) => ({ y: rowHeight(row), x: colWidth(col), color: color(col) }))
    };

    $: maxWidth = Math.max(...svgCircles.map(c => c.x)) + colWidthDefault;
</script>

<svg width={maxWidth}>
    {#each curves as curve}
        <path fill="none" stroke="{curve.color}" stroke-width="1.5" d="{curve.d}"></path>
    {/each}
    {#each svgCircles as c}
        <circle cx={c.x} cy={c.y} r={circleSize} fill={c.color} stroke="none" style=""></circle>
    {/each}
</svg>