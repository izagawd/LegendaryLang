fn main() -> i32 {
    let tt = true && true;
    let tf = true && false;
    let ft = false && true;
    let ff = false && false;
    let result = 0;
    if tt { result = result + 1; };
    if tf { result = result + 10; };
    if ft { result = result + 100; };
    if ff { result = result + 1000; };
    result
}
