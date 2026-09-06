const assert = require('assert');

// 导入原型算法
function createEvaluator() {
  const Hidden = "Hidden", Candidate = "Candidate", Invalid = "LocallyInvalid";
  function insideCursor(input) {
    return input.cursorX >= 0 && input.cursorY >= 0 && input.cursorX < input.gridWidth && input.cursorY < input.gridHeight;
  }
  function fits(input, x, y, w, h) {
    if (x < 0 || y < 0 || x + w > input.gridWidth || y + h > input.gridHeight) return false;
    for (let yy = y; yy < y + h; yy++) for (let xx = x; xx < x + w; xx++) if (input.occupied[yy * input.gridWidth + xx]) return false;
    return true;
  }
  function projected(input, w, h) {
    let maxX = input.gridWidth - w, maxY = input.gridHeight - h;
    if (maxX < 0 || maxY < 0) return { x:0, y:0 };
    let x = Math.floor(input.cursorX - w * 0.5 + 0.5);
    let y = Math.floor(input.cursorY - h * 0.5 + 0.5);
    if (x < 0) x = 0; else if (x > maxX) x = maxX;
    if (y < 0) y = 0; else if (y > maxY) y = maxY;
    return { x, y };
  }
  function search(input, w, h, rotation, counter) {
    if (w > input.gridWidth || h > input.gridHeight) return null;
    let found = false, bestX = 0, bestY = 0, bestDistance = 0;
    for (let y = 0; y <= input.gridHeight - h; y++) {
      for (let x = 0; x <= input.gridWidth - w; x++) {
        counter.value++;
        if (!fits(input, x, y, w, h)) continue;
        const dx = x + w * 0.5 - input.cursorX;
        const dy = y + h * 0.5 - input.cursorY;
        const d = dx * dx + dy * dy;
        if (!found || d < bestDistance || (d === bestDistance && (y < bestY || (y === bestY && x < bestX)))) {
          found = true; bestX = x; bestY = y; bestDistance = d;
        }
      }
    }
    return found ? { x:bestX, y:bestY, width:w, height:h, rotation, distance:bestDistance } : null;
  }
  function evaluate(input) {
    if (!insideCursor(input)) return { state:Hidden, x:0, y:0, width:0, height:0, rotation:input.rotation, reason:"OutsideGrid", orientation:"hidden", checks:0 };
    let w = (input.rotation & 1) === 0 ? input.itemWidth : input.itemHeight;
    let h = (input.rotation & 1) === 0 ? input.itemHeight : input.itemWidth;
    const counter = { value:0 };
    let anyOrientationFitsGrid = w <= input.gridWidth && h <= input.gridHeight;
    const localCurrent = projected(input, w, h);
    counter.value++;
    if (fits(input, localCurrent.x, localCurrent.y, w, h)) {
      return { state:Candidate, x:localCurrent.x, y:localCurrent.y, width:w, height:h, rotation:input.rotation & 3, reason:"None", orientation:"current-local", checks:counter.value };
    }
    let rotated = 0, rotatedW = h, rotatedH = w;
    if (input.autoRotate && input.itemWidth !== input.itemHeight) {
      rotated = (input.rotation + 1) & 3;
      if (rotatedW <= input.gridWidth && rotatedH <= input.gridHeight) anyOrientationFitsGrid = true;
      const localRotated = projected(input, rotatedW, rotatedH);
      counter.value++;
      if (fits(input, localRotated.x, localRotated.y, rotatedW, rotatedH)) {
        return { state:Candidate, x:localRotated.x, y:localRotated.y, width:rotatedW, height:rotatedH, rotation:rotated, reason:"None", orientation:"automatic-90-local", checks:counter.value };
      }
    }
    const currentResult = search(input, w, h, input.rotation & 3, counter);
    if (currentResult) return { state:Candidate, ...currentResult, reason:"None", orientation:"current-expanded", checks:counter.value };
    let rotatedResult = null;
    if (input.autoRotate && input.itemWidth !== input.itemHeight) {
      rotatedResult = search(input, rotatedW, rotatedH, rotated, counter);
    }
    if (rotatedResult) return { state:Candidate, ...rotatedResult, reason:"None", orientation:"automatic-90-expanded", checks:counter.value };
    const p = projected(input, w, h);
    return { state:Invalid, x:p.x, y:p.y, width:w, height:h, rotation:input.rotation & 3, reason:anyOrientationFitsGrid ? "Occupied" : "OutsideGrid", orientation:"current-invalid", checks:counter.value };
  }
  return { evaluate };
}

const evaluator = createEvaluator();

console.log("=== 开始执行 GPT-12 算法与视觉层全面复测 ===");

let passed = 0, failed = 0;
function test(name, fn) {
  try {
    fn();
    console.log(`  [PASS] ${name}`);
    passed++;
  } catch (err) {
    console.error(`  [FAIL] ${name}:`, err.message);
    failed++;
  }
}

// 1. 空白网格高密度平移测试：绝对零翻转零蠕动 (2x3)
test("测试 1：空白网格连续对角线平移 (2x3) 保持 100% current-local 零蠕动 (4524 个样本点)", () => {
  const gridWidth = 8, gridHeight = 6;
  const occupied = new Uint8Array(gridWidth * gridHeight);
  let flipCount = 0;
  for (let x = 0.1; x < 7.9; x += 0.1) {
    for (let y = 0.1; y < 5.9; y += 0.1) {
      const res = evaluator.evaluate({
        gridWidth, gridHeight, itemWidth: 2, itemHeight: 3, rotation: 0,
        autoRotate: true, cursorX: x, cursorY: y, occupied
      });
      if (res.orientation !== "current-local" || res.width !== 2 || res.height !== 3) {
        flipCount++;
      }
    }
  }
  assert.strictEqual(flipCount, 0, `在空白网格平移中发生了 ${flipCount} 次异常翻转！`);
});

// 2. 空白网格高密度平移测试：长条物品 (1x4)
test("测试 2：空白网格连续对角线平移 (1x4) 保持 100% current-local 零蠕动 (4524 个样本点)", () => {
  const gridWidth = 8, gridHeight = 6;
  const occupied = new Uint8Array(gridWidth * gridHeight);
  let flipCount = 0;
  for (let x = 0.1; x < 7.9; x += 0.1) {
    for (let y = 0.1; y < 5.9; y += 0.1) {
      const res = evaluator.evaluate({
        gridWidth, gridHeight, itemWidth: 1, itemHeight: 4, rotation: 0,
        autoRotate: true, cursorX: x, cursorY: y, occupied
      });
      if (res.orientation !== "current-local" || res.width !== 1 || res.height !== 4) {
        flipCount++;
      }
    }
  }
  assert.strictEqual(flipCount, 0, `在空白网格平移中发生了 ${flipCount} 次异常翻转！`);
});

// 3. 用户关注的核心场景：障碍物上方空隙就地旋转
test("测试 3：中央障碍物 (2..4, 2..4)，上方有狭缝，光标在 (3.5, 1.1) 触发 automatic-90-local", () => {
  const gridWidth = 8, gridHeight = 6;
  const occupied = new Uint8Array(gridWidth * gridHeight);
  for (let y = 2; y <= 4; y++) {
    for (let x = 2; x <= 4; x++) {
      occupied[y * gridWidth + x] = 1;
    }
  }
  const res = evaluator.evaluate({
    gridWidth, gridHeight, itemWidth: 2, itemHeight: 3, rotation: 0,
    autoRotate: true, cursorX: 3.5, cursorY: 1.1, occupied
  });
  assert.strictEqual(res.state, "Candidate");
  assert.strictEqual(res.orientation, "automatic-90-local");
  assert.strictEqual(res.width, 3);
  assert.strictEqual(res.height, 2);
  assert.strictEqual(res.y, 0); // 吸附在第 0 行 (0~1 行放得下 3x2)
});

// 4. 关闭自动旋转对比测试
test("测试 4：同一障碍场景下关闭自动旋转 (autoRotate=false) 必须降级为 current-expanded 保持 2x3", () => {
  const gridWidth = 8, gridHeight = 6;
  const occupied = new Uint8Array(gridWidth * gridHeight);
  for (let y = 2; y <= 4; y++) {
    for (let x = 2; x <= 4; x++) {
      occupied[y * gridWidth + x] = 1;
    }
  }
  const res = evaluator.evaluate({
    gridWidth, gridHeight, itemWidth: 2, itemHeight: 3, rotation: 0,
    autoRotate: false, cursorX: 3.5, cursorY: 1.1, occupied
  });
  assert.strictEqual(res.state, "Candidate");
  assert.strictEqual(res.orientation, "current-expanded");
  assert.strictEqual(res.width, 2);
  assert.strictEqual(res.height, 3);
});

// 5. 边缘靠齐测试 (1x5 长条贴紧右下角)
test("测试 5：长条物品 (1x5) 贴紧右下角 (7.8, 5.7) 自动约束在边界内", () => {
  const gridWidth = 8, gridHeight = 6;
  const occupied = new Uint8Array(gridWidth * gridHeight);
  const res = evaluator.evaluate({
    gridWidth, gridHeight, itemWidth: 1, itemHeight: 5, rotation: 0,
    autoRotate: true, cursorX: 7.8, cursorY: 5.7, occupied
  });
  assert.strictEqual(res.state, "Candidate");
  assert.strictEqual(res.x, 7);
  assert.strictEqual(res.y, 1); // 6 - 5 = 1
  assert.strictEqual(res.width, 1);
  assert.strictEqual(res.height, 5);
});

// 6. 容器满载无解测试
test("测试 6：全网格满载 (3x3 物品) 返回 LocallyInvalid / Occupied", () => {
  const gridWidth = 8, gridHeight = 6;
  const occupied = new Uint8Array(gridWidth * gridHeight);
  occupied.fill(1); // 全满
  const res = evaluator.evaluate({
    gridWidth, gridHeight, itemWidth: 3, itemHeight: 3, rotation: 0,
    autoRotate: true, cursorX: 3.5, cursorY: 2.5, occupied
  });
  assert.strictEqual(res.state, "LocallyInvalid");
  assert.strictEqual(res.reason, "Occupied");
});

// 7. 容器超尺寸测试 (9x7 物品在 8x6 网格双向均放不下)
test("测试 7：超尺寸物品 (9x7 在 8x6 网格) 返回 LocallyInvalid / OutsideGrid", () => {
  const gridWidth = 8, gridHeight = 6;
  const occupied = new Uint8Array(gridWidth * gridHeight);
  const res = evaluator.evaluate({
    gridWidth, gridHeight, itemWidth: 9, itemHeight: 7, rotation: 0,
    autoRotate: true, cursorX: 3.5, cursorY: 2.5, occupied
  });
  assert.strictEqual(res.state, "LocallyInvalid");
  assert.strictEqual(res.reason, "OutsideGrid");
});

// 8. 光标移出容器测试
test("测试 8：光标移出容器 (cursorX = -1) 返回 Hidden / OutsideGrid", () => {
  const gridWidth = 8, gridHeight = 6;
  const occupied = new Uint8Array(gridWidth * gridHeight);
  const res = evaluator.evaluate({
    gridWidth, gridHeight, itemWidth: 2, itemHeight: 3, rotation: 0,
    autoRotate: true, cursorX: -1, cursorY: -1, occupied
  });
  assert.strictEqual(res.state, "Hidden");
  assert.strictEqual(res.reason, "OutsideGrid");
});

console.log(`\n=== 测试结果: ${passed} 通过, ${failed} 失败 ===`);
