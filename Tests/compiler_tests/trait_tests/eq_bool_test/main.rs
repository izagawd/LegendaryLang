fn main() -> i32 {
    let a = true == true;
    let b = true == false;
    let c = false == false;
    let result = 0;
    if a { result = result + 1; };
    if b { result = result + 10; };
    if c { result = result + 100; };
    result
}
