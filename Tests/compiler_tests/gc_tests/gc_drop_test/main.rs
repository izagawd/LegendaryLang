fn main() -> i32 {
    {
        let b: GcMut(i32) = GcMut.New(99);
    }
    5
}
