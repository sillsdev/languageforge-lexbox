<script lang="ts" module>
  export type Circle = { row: number; col: number };
  export type Path = { fromIdx: number; toIdx: number }; // Indices into array of circles
  type SVGDot = { x: number; y: number; color?: string };
  type SVGStroke = { color?: string; d: string };
</script>

<script lang="ts">
  // We need a list of dots and lines, i.e. which dots go to which parents

  interface Props {
    // Each dot has one or two parents, and needs a Bezier curve to the parent
    // Format is [{fromIdx:0,toIdx:1},{fromIdx:1,toIdx:2},{fromIdx:1,toIdx:3}], and so on. Dot 0 connects to dot 1. Dot 1 connects to both dots 2 and 3, because it was a fork or a merge.
    paths?: Path[];
    // Format is [{row:0,col:0},{row:1,col:0},{row:2,col:0},{row:2,col:1}]. Each dot has a row and column, which are auto-translated to x and y
    circles?: Circle[];
    rowHeights?: number[];
    firstRowOffset?: number;
    firstColOffset?: number;
    colWidthDefault?: number; // May be auto-calculated in the future
    rowHeightDefault?: number;
    circleSize?: number;
    colors?: string[];
  }

  const {
    paths = [],
    circles = [],
    rowHeights = [],
    firstRowOffset = 10,
    firstColOffset = 10,
    colWidthDefault = 10,
    rowHeightDefault = 20,
    circleSize = 5,
    colors = [
      // Default set of colors works nicely, but allow overriding if needed
      '#4e79a7',
      '#f28e2c',
      '#e15759',
      '#76b7b2',
      '#59a14f',
      '#edc949',
      '#af7aa1',
      '#ff9da7',
      '#9c755f',
      '#bab0ab',
    ],
  }: Props = $props();
  let colorLength = $derived(colors.length);
  function color(colIdx: number): string {
    return colors[colIdx % colorLength];
  }

  function calculateCumulativeHeights(simpleHeights: number[]): number[] {
    let firstRow = simpleHeights && simpleHeights.length > 0 ? simpleHeights[0] / 2 : firstRowOffset;
    let total = firstRow;
    let result = [firstRow];
    for (const height of simpleHeights) {
      total += height;
      result.push(total);
    }
    return result;
  }
  let cumulativeHeights = $derived(calculateCumulativeHeights(rowHeights));

  function bezier(from: SVGDot, to: SVGDot): SVGStroke {
    /*
    - If parent was in same column, M (child X,Y) and then V (parent Y)
    - If no parent, vertical line to bottom of graph (same as above but V (bottom-of-graph Y) instead of parent Y) - TODO
    - If parent was in different column, Bezier curve as follows:
      - Calculate halfway-point between parent and child. Call it Hx, Hy. Cx, Cy is child, and Px, Py is parent.
      - M Cx, Cy
      - S Cx,Hy Hx,Hy (starting point of Cx, Cy is implied in SVG S command)
      - S Px,Hy Px,Py (starting point of Hx, Hy is implied in SVG S command)
      - Note that parents are *below* children in this graph, so Hy is below Cy and above Py
    */
    let { x: fromX, y: fromY, color: strokeColor } = from;
    if (to) {
      let { x: toX, y: toY } = to;
      if (fromX == toX) {
        return { color: strokeColor, d: `M${fromX} ${fromY}V${toY}` };
      } else {
        let halfX = (fromX + toX) / 2;
        let halfY = (fromY + toY) / 2;
        return {
          color: strokeColor,
          d: `M${fromX} ${fromY}S${fromX} ${halfY},${halfX} ${halfY}S${toX} ${halfY},${toX} ${toY}`,
        };
      }
    } else return { color: strokeColor, d: '' }; // TODO: Path should be V${bottomY} to draw a line to the bottom of the graph, but how can we know bottomY?
  }

  let rowHeight = $derived((rowIdx: number): number => {
    return cumulativeHeights[rowIdx] ? cumulativeHeights[rowIdx] : rowHeightDefault * rowIdx + firstRowOffset;
  });

  let colWidth = $derived((colIdx: number): number => {
    return colWidthDefault * colIdx + firstColOffset;
  });

  let svgDots: SVGDot[] = $derived(
    circles.map(({ row, col }) => ({
      y: rowHeight(row),
      x: colWidth(col),
      color: color(col),
    })),
  );
  let curves: SVGStroke[] = $derived(paths.map(({ fromIdx: f, toIdx: t }) => bezier(svgDots[f], svgDots[t])));

  let maxWidth = $derived(Math.max(...svgDots.map((c) => c.x)) + colWidthDefault);
</script>

{#if circles?.length > 0}
  <svg width={maxWidth} height="0" style="height: 100%">
    {#if rowHeights?.length > 0}
      {#each curves as curve}
        <path fill="none" stroke={curve.color} stroke-width="1.5" d={curve.d} />
      {/each}
      {#each svgDots as c}
        <circle cx={c.x} cy={c.y} r={circleSize} fill={c.color} stroke="none" style="" />
      {/each}
    {/if}
  </svg>
{/if}
