fn main() -> i32 {
    let a = 42;
    let b = 42;
    let ra = &mut a;
    let rb = &mut b;
    if ra == rb { 1 } else { 0 }
}
