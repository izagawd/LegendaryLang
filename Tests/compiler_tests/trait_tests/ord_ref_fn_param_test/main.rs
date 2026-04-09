fn is_less(a: &i32, b: &i32) -> bool {
    a < b
}

fn main() -> i32 {
    let x = 3;
    let y = 7;
    let r1 = if is_less(&x, &y) { 1 } else { 0 };
    let r2 = if is_less(&y, &x) { 10 } else { 0 };
    r1 + r2
}
