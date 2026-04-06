fn is_valid(x: i32) -> bool {
    x > 0 && x < 100 && x != 50
}

fn main() -> i32 {
    let r1 = if is_valid(25) { 1 } else { 0 };
    let r2 = if is_valid(50) { 10 } else { 0 };
    let r3 = if is_valid(0) { 100 } else { 0 };
    let r4 = if is_valid(100) { 1000 } else { 0 };
    r1 + r2 + r3 + r4
}
