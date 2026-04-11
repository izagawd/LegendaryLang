fn main() -> i32 {
    let a: GcMut(i32) = GcMut.New(10);
    let b: GcMut(i32) = GcMut.New(32);
    *a + *b
}
