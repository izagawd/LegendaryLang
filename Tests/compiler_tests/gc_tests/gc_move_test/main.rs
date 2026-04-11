fn take_box(b: GcMut(i32)) -> i32 {
    *b
}

fn main() -> i32 {
    let b: GcMut(i32) = GcMut.New(77);
    take_box(b)
}
