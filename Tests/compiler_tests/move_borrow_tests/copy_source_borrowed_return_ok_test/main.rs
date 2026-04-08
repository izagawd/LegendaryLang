struct Holder['a] { r: &'a i32 }

fn main() -> i32 {
    let a = 5;
    let dd = make Holder { r: &a };
    a
}
