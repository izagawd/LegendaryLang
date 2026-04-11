fn main() -> i32 {
    let b: Gc(i32) = Gc.New(7);
    let x: i32 = *b;
    let y: i32 = *b;
    let z: i32 = *b;
    x + y + z
}
