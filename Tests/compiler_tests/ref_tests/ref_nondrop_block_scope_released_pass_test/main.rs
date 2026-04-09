struct Holder['a] {
    r: &'a uniq i32
}

fn main() -> i32 {
    let a = 5;
    let val = {
        let h = make Holder { r: &uniq a };
        42
    };
    a + val
}
