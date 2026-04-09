fn are_equal(a: &i32, b: &i32) -> bool {
    a == b
}

fn main() -> i32 {
    let x = 5;
    let y = 5;
    let z = 9;
    let r1 = if are_equal(&x, &y) { 1 } else { 0 };
    let r2 = if are_equal(&x, &z) { 10 } else { 0 };
    r1 + r2
}
