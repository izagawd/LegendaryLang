fn main() -> i32 {
    let a = 42;
    let b = 42;
    let c = 99;
    let r1 = if a == b { 1 } else { 0 };
    let r2 = if a == c { 1 } else { 0 };
    r1 + r2
}
