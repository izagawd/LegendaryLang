fn main() -> i32 {
    let b: GcMut(i32) = GcMut.New(7);
    let x: i32 = *b;
    let y: i32 = *b;
    let z: i32 = *b;
    x + y + z
}
