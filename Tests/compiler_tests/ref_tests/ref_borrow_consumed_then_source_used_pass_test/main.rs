struct Holder['a] {
    r: &'a mut i32
}

fn DropIt[T:! type](x: T) {}

fn main() -> i32 {
    let a = 5;
    let h = make Holder { r: &mut a };
    DropIt(h);
    a
}
