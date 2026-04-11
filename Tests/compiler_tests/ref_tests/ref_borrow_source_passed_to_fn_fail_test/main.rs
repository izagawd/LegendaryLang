struct Holder['a] {
    r: &'a mut i32
}

fn read(x: i32) -> i32 { x }

fn main() -> i32 {
    let a = 5;
    let h = make Holder { r: &mut a };
    read(a)
}
