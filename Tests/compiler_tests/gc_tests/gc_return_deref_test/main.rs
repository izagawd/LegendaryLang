fn get_val(b: GcMut(i32)) -> i32 {
    *b
}

fn main() -> i32 {
    let a: i32 = get_val(GcMut.New(10));
    let b: i32 = get_val(GcMut.New(32));
    a + b
}
