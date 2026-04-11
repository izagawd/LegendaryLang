struct Holder['a] {
    val: &'a mut i32
}

fn DropNow[T:! Sized](input: T) {}

fn main() -> i32 {
    let x = 10;
    let h = make Holder { val: &mut x };
    DropNow(h);
    x
}
