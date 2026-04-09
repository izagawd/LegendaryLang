fn is_sorted3(a: i32, b: i32, c: i32) -> bool {
    a <= b && b <= c
}

fn main() -> i32 {
    let r1 = if is_sorted3(1, 2, 3) { 1 } else { 0 };
    let r2 = if is_sorted3(1, 1, 1) { 10 } else { 0 };
    let r3 = if is_sorted3(3, 2, 1) { 100 } else { 0 };
    let r4 = if is_sorted3(1, 3, 2) { 1000 } else { 0 };
    r1 + r2 + r3 + r4
}
