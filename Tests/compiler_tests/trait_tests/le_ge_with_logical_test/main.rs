fn in_range_inclusive(x: i32, low: i32, high: i32) -> bool {
    x >= low && x <= high
}

fn main() -> i32 {
    let r1 = if in_range_inclusive(5, 1, 10) { 1 } else { 0 };
    let r2 = if in_range_inclusive(1, 1, 10) { 10 } else { 0 };
    let r3 = if in_range_inclusive(10, 1, 10) { 100 } else { 0 };
    let r4 = if in_range_inclusive(0, 1, 10) { 1000 } else { 0 };
    let r5 = if in_range_inclusive(11, 1, 10) { 10000 } else { 0 };
    r1 + r2 + r3 + r4 + r5
}
