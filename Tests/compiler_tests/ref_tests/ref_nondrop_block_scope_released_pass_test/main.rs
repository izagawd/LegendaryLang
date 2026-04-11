struct Holder['a] {
    r: &'a mut i32
}

fn main() -> i32 {
    let a = 5;
    let val = {
        let h = make Holder { r: &mut a };
        42
    };
    a + val
}
