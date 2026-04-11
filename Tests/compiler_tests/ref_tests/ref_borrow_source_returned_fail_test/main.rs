struct Yo['a]{
    dd: &'a mut i32
}

fn main() -> i32 {
    let a = 5;
    let borrow = make Yo{ dd: &mut a };
    a
}
