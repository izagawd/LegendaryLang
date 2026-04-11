struct Holder['a] {
    r: &'a mut i32
}

fn Pass[T:! Sized](x: T) -> T { x }
fn DropIt[T:! Sized](x: T) {}

fn main() -> i32 {
    let a = 5;
    let h = make Holder { r: &mut a };
    let h2 = Pass(Pass(h));
    DropIt(h2);
    a
}
