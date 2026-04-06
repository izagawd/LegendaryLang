fn main() -> i32 {
    let a = 10;
    let b = 20;
    let ra = &a;
    let rb = &b;
    if ra != rb { 1 } else { 0 }
}
