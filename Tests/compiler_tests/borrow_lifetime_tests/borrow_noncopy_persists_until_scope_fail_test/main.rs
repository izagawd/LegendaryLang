struct Holder['a] {
    val: &'a mut i32
}

fn main() -> i32 {
    let x = 10;
    let h = make Holder { val: &mut x };
    x
}
